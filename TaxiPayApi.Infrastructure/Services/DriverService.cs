using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.Common.Telemetry;
using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Application.DTOs.Realtime;
using TaxiPayApi.Application.Interfaces;
using TaxiPayApi.Domain.Entities;
using TaxiPayApi.Infrastructure.Persistence.Context;

namespace TaxiPayApi.Application.Services;

public class DriverService(
    TaxiPayDbContext _dbContext,
    UserManager<TaxiPayUser> _userManager,
    IMapper _mapper,
    IOptions<JwtSettings> jwtSettings,
    ITaxiPayRealtimeNotifier _realtimeNotifier,
    INotificationService _notificationService,
    ICurrentUserService? _currentUserService = null) : IDriverService
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    public async Task<Result<DriverResponse, DriverError>> CreateDriverAsync(
        CreateDriverRequest request,
        CancellationToken cancellationToken = default)
    {
        var phone = request.Phone.Trim();
        var existingUser = await _userManager.FindByNameAsync(phone)
            ?? await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone, cancellationToken);

        if (existingUser != null || await _dbContext.Drivers.AnyAsync(d => d.Phone == phone, cancellationToken))
            return Result<DriverResponse, DriverError>.Failure(DriverError.PhoneAlreadyExists());

        var licenseExists = await _dbContext.Drivers
            .AnyAsync(d => d.LicenseNumber == request.LicenseNumber, cancellationToken);

        if (licenseExists)
            return Result<DriverResponse, DriverError>.Failure(DriverError.LicenseAlreadyExists());

        Guid? routeId = null;
        string? routeName = null;

        if (request.PreferredRouteId.HasValue)
        {
            var route = await _dbContext.Routes
                .FirstOrDefaultAsync(r => r.Id == request.PreferredRouteId.Value && r.IsActive, cancellationToken);

            if (route == null)
                return Result<DriverResponse, DriverError>.Failure(DriverError.RouteNotFound(request.PreferredRouteId.Value));

            routeId = route.Id;
            routeName = route.Name;
        }

        var user = new TaxiPayUser
        {
            UserName = phone,
            PhoneNumber = phone,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            PhoneNumberConfirmed = true,
            CreatedAt = DateTime.UtcNow
        };

        var identityResult = await _userManager.CreateAsync(user, request.Password);
        if (!identityResult.Succeeded)
        {
            var errorMsg = identityResult.Errors.FirstOrDefault()?.Description ?? "Failed to create driver identity account.";
            return Result<DriverResponse, DriverError>.Failure(DriverError.ValidationFailed(errorMsg));
        }

        await _userManager.AddToRoleAsync(user, "Driver");

        var driver = _mapper.Map<Driver>(request);
        driver.UserId = user.Id;
        driver.PreferredRouteId = routeId;
        driver.CurrentRouteId = routeId;
        driver.Status = DriverStatus.Pending;

        _dbContext.Drivers.Add(driver);
        await _dbContext.SaveChangesAsync(cancellationToken);

        await _realtimeNotifier.NotifyDriverStatusUpdatedAsync(
            driver.Id,
            driver.DriverId,
            new DriverStatusUpdateDto(driver.Id, driver.Status.ToString(), driver.IsVerified, driver.DriverId, null),
            cancellationToken);

        var response = _mapper.Map<DriverResponse>(driver);
        response = response with { CurrentRouteName = routeName };

        return Result<DriverResponse, DriverError>.Success(response);
    }

    public async Task<Result<LoginResponse, DriverError>> LoginAsync(
       DriverLoginRequest request,
       CancellationToken cancellationToken = default)
    {
        var identifier = request.Phone.Trim();

        var driver = await _dbContext.Drivers
            .Include(d => d.CurrentRoute)
            .FirstOrDefaultAsync(d => d.Phone == identifier || d.DriverId == identifier, cancellationToken);

        TaxiPayUser? user = null;
        if (driver != null && !string.IsNullOrEmpty(driver.UserId))
        {
            user = await _userManager.FindByIdAsync(driver.UserId);
        }

        if (user == null)
        {
            user = await _userManager.FindByNameAsync(identifier)
                ?? await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == identifier || u.Email == identifier, cancellationToken);
        }

        if (user == null)
            return Result<LoginResponse, DriverError>.Failure(DriverError.InvalidCredentials());

        if (await _userManager.IsLockedOutAsync(user))
        {
            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
            var remainingMinutes = lockoutEnd.HasValue
                ? Math.Max(1, (int)Math.Ceiling((lockoutEnd.Value - DateTimeOffset.UtcNow).TotalMinutes))
                : 5;
            return Result<LoginResponse, DriverError>.Failure(DriverError.AccountLockedOut(remainingMinutes));
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        if (!isPasswordValid)
        {
            await _userManager.AccessFailedAsync(user);
            if (await _userManager.IsLockedOutAsync(user))
            {
                var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
                var remainingMinutes = lockoutEnd.HasValue
                    ? Math.Max(1, (int)Math.Ceiling((lockoutEnd.Value - DateTimeOffset.UtcNow).TotalMinutes))
                    : 5;
                return Result<LoginResponse, DriverError>.Failure(DriverError.AccountLockedOut(remainingMinutes));
            }

            return Result<LoginResponse, DriverError>.Failure(DriverError.InvalidCredentials());
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        if (driver == null)
        {
            driver = await _dbContext.Drivers
                .Include(d => d.CurrentRoute)
                .FirstOrDefaultAsync(d => d.UserId == user.Id || d.Phone == user.PhoneNumber || d.Phone == identifier, cancellationToken);
        }

        if (driver == null)
            return Result<LoginResponse, DriverError>.Failure(DriverError.InvalidCredentials());

        if (string.IsNullOrEmpty(driver.UserId))
        {
            driver.UserId = user.Id;
        }

        if (driver.Status == DriverStatus.Rejected)
            return Result<LoginResponse, DriverError>.Failure(DriverError.DriverRejected(driver.RejectionReason));

        if (driver.Status == DriverStatus.Pending)
            return Result<LoginResponse, DriverError>.Failure(DriverError.DriverNotApproved());

        if (driver.Status == DriverStatus.Suspended)
            return Result<LoginResponse, DriverError>.Failure(DriverError.DriverSuspended());

        driver.LastLoginAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var (accessToken, refreshToken, expiresAt) = GenerateTokens(driver, user);

        var activeShift = await _dbContext.DriverShifts
            .FirstOrDefaultAsync(s => s.DriverId == driver.Id &&
                                      s.Status == ShiftStatus.Active, cancellationToken);

        var response = new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenExpiresAt = expiresAt,
            LoginTime = DateTime.UtcNow,
            SessionId = Guid.NewGuid().ToString(),
            IsFirstLogin = driver.TotalTrips == 0,
            RequiresPasswordChange = false,
            HasPendingRouteChange = driver.RouteChangeRequests.Any(r => r.Status == RouteChangeStatus.Pending),
            Driver = _mapper.Map<DriverSummaryDto>(driver),
            ActiveShift = activeShift != null ? new ShiftStatusDto
            {
                ShiftId = activeShift.Id,
                StartTime = activeShift.ShiftStart,
                CurrentTripNumber = activeShift.CurrentTripNumber,
                TotalRevenue = activeShift.TotalRevenue,
                TotalPassengers = activeShift.TotalPassengers,
                IsActive = true
            } : null
        };

        return Result<LoginResponse, DriverError>.Success(response);
    }

    private (string accessToken, string refreshToken, DateTime expiresAt) GenerateTokens(Driver driver, TaxiPayUser user)
    {
        var expiresAt = DateTime.UtcNow.AddHours(_jwtSettings.AccessTokenExpiryHours);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim("driverGuid", driver.Id.ToString()),
            new Claim("driverId", driver.DriverId ?? string.Empty),
            new Claim(ClaimTypes.Name, $"{driver.FirstName} {driver.LastName}"),
            new Claim(ClaimTypes.MobilePhone, driver.Phone),
            new Claim(ClaimTypes.Role, "Driver"),
            new Claim("role", "driver")
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtSettings.Issuer,
            audience: _jwtSettings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: creds
        );

        var accessToken = new JwtSecurityTokenHandler().WriteToken(token);
        var refreshToken = Guid.NewGuid().ToString();

        return (accessToken, refreshToken, expiresAt);
    }



    public async Task<Result<DriverResponse, DriverError>> GetProfileAsync(
        Guid driverId,
        CancellationToken cancellationToken = default)
    {
        if (_currentUserService != null && _currentUserService.IsAuthenticated && !_currentUserService.IsAdmin && _currentUserService.DriverGuid.HasValue && _currentUserService.DriverGuid.Value != driverId)
        {
            return Result<DriverResponse, DriverError>.Failure(DriverError.Forbidden("You do not have permission to view another driver's profile."));
        }

        var driver = await _dbContext.Drivers
            .Include(d => d.CurrentRoute)
            .FirstOrDefaultAsync(d => d.Id == driverId, cancellationToken);

        if (driver == null)
            return Result<DriverResponse, DriverError>.Failure(DriverError.DriverNotFound(driverId));

        var response = _mapper.Map<DriverResponse>(driver);
        response = response with
        {
            CurrentRouteName = driver.CurrentRoute?.Name
        };

        return Result<DriverResponse, DriverError>.Success(response);
    }

    public async Task<Result<DriverResponse, DriverError>> UpdateProfileAsync(
        Guid driverId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_currentUserService != null && _currentUserService.IsAuthenticated && !_currentUserService.IsAdmin && _currentUserService.DriverGuid.HasValue && _currentUserService.DriverGuid.Value != driverId)
        {
            return Result<DriverResponse, DriverError>.Failure(DriverError.Forbidden("You do not have permission to update another driver's profile."));
        }

        var driver = await _dbContext.Drivers
            .FirstOrDefaultAsync(d => d.Id == driverId, cancellationToken);

        if (driver == null)
            return Result<DriverResponse, DriverError>.Failure(DriverError.DriverNotFound(driverId));

        if (driver.Status == DriverStatus.Suspended)
            return Result<DriverResponse, DriverError>.Failure(DriverError.CannotUpdateSuspendedDriver());

        if (driver.Status == DriverStatus.Pending)
            return Result<DriverResponse, DriverError>.Failure(DriverError.CannotUpdatePendingDriver());

        if (request.Phone != driver.Phone)
        {
            var phoneExists = await _dbContext.Drivers
                .AnyAsync(d => d.Phone == request.Phone && d.Id != driverId, cancellationToken)
                || await _userManager.Users.AnyAsync(u => u.PhoneNumber == request.Phone && u.Id != driver.UserId, cancellationToken);

            if (phoneExists)
                return Result<DriverResponse, DriverError>.Failure(DriverError.PhoneAlreadyInUse());
        }

        driver.FirstName = request.FirstName.Trim();
        driver.LastName = request.LastName.Trim();
        driver.Phone = request.Phone.Trim();
        driver.PhotoUrl = request.PhotoUrl;
        driver.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(driver.UserId))
        {
            var user = await _userManager.FindByIdAsync(driver.UserId);
            if (user != null)
            {
                user.FirstName = driver.FirstName;
                user.LastName = driver.LastName;
                user.PhoneNumber = driver.Phone;
                user.UserName = driver.Phone;
                user.UpdatedAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = _mapper.Map<DriverResponse>(driver);

        return Result<DriverResponse, DriverError>.Success(response);
    }

    public async Task<Result<RouteChangeResponse, DriverError>> RequestRouteChangeAsync(
        Guid driverId,
        RequestRouteChangeRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_currentUserService != null && _currentUserService.IsAuthenticated && !_currentUserService.IsAdmin && _currentUserService.DriverGuid.HasValue && _currentUserService.DriverGuid.Value != driverId)
        {
            return Result<RouteChangeResponse, DriverError>.Failure(DriverError.Forbidden("You do not have permission to request a route change for another driver."));
        }

        var driver = await _dbContext.Drivers
            .Include(d => d.CurrentRoute)
            .FirstOrDefaultAsync(d => d.Id == driverId, cancellationToken);

        if (driver == null)
            return Result<RouteChangeResponse, DriverError>.Failure(
                DriverError.DriverNotFound(driverId));

        if (driver.Status == DriverStatus.Suspended)
            return Result<RouteChangeResponse, DriverError>.Failure(
                DriverError.CannotRequestRouteChangeForSuspendedDriver());

        if (driver.Status == DriverStatus.Pending)
            return Result<RouteChangeResponse, DriverError>.Failure(
                DriverError.CannotRequestRouteChangeForPendingDriver());

        var hasPendingRequest = await _dbContext.RouteChangeRequests
            .AnyAsync(r => r.DriverId == driverId &&
                          r.Status == RouteChangeStatus.Pending,
                          cancellationToken);

        var targetRouteId = request.EffectiveNewRouteId;

        if (hasPendingRequest)
            return Result<RouteChangeResponse, DriverError>.Failure(
                DriverError.RouteChangeAlreadyRequested());

        if (driver.CurrentRouteId == targetRouteId)
            return Result<RouteChangeResponse, DriverError>.Failure(
                DriverError.SameRouteRequested());

        var newRoute = await _dbContext.Routes
            .FirstOrDefaultAsync(r => r.Id == targetRouteId && r.IsActive,
                cancellationToken);

        if (newRoute == null)
            return Result<RouteChangeResponse, DriverError>.Failure(
                DriverError.RouteNotFoundForChange(targetRouteId));

        var currentRouteId = driver.CurrentRouteId ?? driver.PreferredRouteId ?? targetRouteId;

        var routeChangeRequest = new RouteChangeRequest
        {
            DriverId = driverId,
            CurrentRouteId = currentRouteId,
            NewRouteId = targetRouteId,
            Reason = request.Reason.Trim(),
            Status = RouteChangeStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };

        _dbContext.RouteChangeRequests.Add(routeChangeRequest);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = new RouteChangeResponse
        {
            RequestId = routeChangeRequest.Id,
            DriverId = driver.Id,
            DriverName = $"{driver.FirstName} {driver.LastName}",
            CurrentRoute = driver.CurrentRoute != null ? _mapper.Map<RouteSummaryDto>(driver.CurrentRoute) : null,
            RequestedRoute = _mapper.Map<RouteSummaryDto>(newRoute),
            Reason = routeChangeRequest.Reason,
            Status = routeChangeRequest.Status.ToString(),
            RequestedAt = routeChangeRequest.RequestedAt
        };

        // Real-time broadcast to admins
        var adminDto = new TaxiPayApi.Application.DTOs.Admin.AdminRouteChangeRequestDto(
            Id: routeChangeRequest.Id,
            DriverId: driver.Id,
            DriverName: $"{driver.FirstName} {driver.LastName}",
            DriverCode: driver.DriverId ?? "Driver",
            DriverPhone: driver.Phone,
            CurrentRouteId: currentRouteId,
            CurrentRouteCode: driver.CurrentRoute?.RouteCode ?? "Unknown",
            CurrentRouteName: driver.CurrentRoute?.Name ?? "Unknown",
            NewRouteId: newRoute.Id,
            NewRouteCode: newRoute.RouteCode,
            NewRouteName: newRoute.Name,
            Reason: routeChangeRequest.Reason,
            Status: routeChangeRequest.Status.ToString(),
            HasActiveTrip: false,
            RequestedAt: routeChangeRequest.RequestedAt
        );

        await _realtimeNotifier.NotifyDriverRouteChangeSubmittedAsync(adminDto, cancellationToken);

        await _notificationService.CreateNotificationAsync(
            userId: null,
            role: "Admin",
            title: "New Corridor Transfer Request",
            message: $"Driver {driver.FirstName} {driver.LastName} requested transfer to {newRoute.RouteCode} ({newRoute.Name}).",
            type: "info",
            targetUrl: "/admin/route-requests",
            ct: cancellationToken);

        return Result<RouteChangeResponse, DriverError>.Success(response);
    }

    public async Task<Result<RouteChangeResponse?, DriverError>> GetCurrentRouteChangeRequestAsync(
        Guid driverId,
        CancellationToken cancellationToken = default)
    {
        if (_currentUserService != null && _currentUserService.IsAuthenticated && !_currentUserService.IsAdmin && _currentUserService.DriverGuid.HasValue && _currentUserService.DriverGuid.Value != driverId)
        {
            return Result<RouteChangeResponse?, DriverError>.Failure(DriverError.Forbidden("You do not have permission to view another driver's route change request."));
        }

        var driver = await _dbContext.Drivers.FirstOrDefaultAsync(d => d.Id == driverId, cancellationToken);
        if (driver == null)
            return Result<RouteChangeResponse?, DriverError>.Failure(DriverError.DriverNotFound(driverId));

        var req = await _dbContext.RouteChangeRequests
            .Include(r => r.CurrentRoute)
            .Include(r => r.NewRoute)
            .Where(r => r.DriverId == driverId)
            .OrderByDescending(r => r.RequestedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (req == null)
            return Result<RouteChangeResponse?, DriverError>.Success(null);

        var response = new RouteChangeResponse
        {
            RequestId = req.Id,
            DriverId = driver.Id,
            DriverName = $"{driver.FirstName} {driver.LastName}",
            CurrentRoute = req.CurrentRoute != null ? _mapper.Map<RouteSummaryDto>(req.CurrentRoute) : null,
            RequestedRoute = req.NewRoute != null ? _mapper.Map<RouteSummaryDto>(req.NewRoute) : null,
            Reason = req.Reason,
            Status = req.Status.ToString(),
            RequestedAt = req.RequestedAt,
            ReviewedAt = req.ReviewedAt,
            RejectionReason = req.RejectionReason
        };

        return Result<RouteChangeResponse?, DriverError>.Success(response);
    }

    public async Task<Result<DriverDashboardResponse, DriverError>> GetDashboardAsync(
           Guid driverId,
           CancellationToken cancellationToken = default)
    {
        if (_currentUserService != null && _currentUserService.IsAuthenticated && !_currentUserService.IsAdmin && _currentUserService.DriverGuid.HasValue && _currentUserService.DriverGuid.Value != driverId)
        {
            return Result<DriverDashboardResponse, DriverError>.Failure(DriverError.Forbidden("You do not have permission to view another driver's dashboard."));
        }

        var driver = await _dbContext.Drivers
            .Include(d => d.CurrentRoute)
            .FirstOrDefaultAsync(d => d.Id == driverId && !d.IsDeleted, cancellationToken);

        if (driver == null)
            return Result<DriverDashboardResponse, DriverError>.Failure(
                DriverError.DriverNotFound(driverId));

        var activeShift = await _dbContext.DriverShifts
            .Include(s => s.Trips)
                .ThenInclude(t => t.PassengerJourneys)
                    .ThenInclude(pj => pj.Passenger)
            .Include(s => s.Trips)
                .ThenInclude(t => t.Route)
            .Include(s => s.Route)
            .FirstOrDefaultAsync(s => s.DriverId == driverId &&
                                      s.Status == ShiftStatus.Active, cancellationToken);

        if (activeShift == null && driver.CurrentRouteId.HasValue)
        {
            activeShift = new DriverShift
            {
                DriverId = driver.Id,
                RouteId = driver.CurrentRouteId.Value,
                ShiftStart = DateTime.UtcNow,
                Status = ShiftStatus.Active,
                CurrentTripNumber = 0,
                TotalPassengers = 0,
                TotalRevenue = 0,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.DriverShifts.Add(activeShift);
            await _dbContext.SaveChangesAsync(cancellationToken);
            activeShift.Route = driver.CurrentRoute;
        }

        List<ActivePassengerDto> activePassengers = new();
        int activePassengerCount = 0;
        decimal shiftRevenue = 0;
        int shiftPassengers = 0;
        int currentTripNumber = 0;
        Guid? activeShiftId = null;
        Guid? activeTripId = null;
        bool isTripActive = false;
        DateTime shiftStart = DateTime.UtcNow;
        ActiveTripDto? activeTripDto = null;
        CompletedTripDto? lastCompletedTripDto = null;

        if (activeShift != null)
        {
            activeShiftId = activeShift.Id;
            shiftStart = activeShift.ShiftStart;
            shiftRevenue = activeShift.TotalRevenue;
            shiftPassengers = activeShift.TotalPassengers;
            currentTripNumber = activeShift.CurrentTripNumber;

            var currentTrip = activeShift.Trips
                .FirstOrDefault(t => t.Status == TripStatus.Active);

            if (currentTrip != null)
            {
                activeTripId = currentTrip.Id;
                isTripActive = true;
                activePassengers = currentTrip.PassengerJourneys
                    .Where(pj => pj.PaymentStatus == PaymentStatus.Completed &&
                                 pj.AlightingStatus != AlightingStatus.Alighted)
                    .Select(pj =>
                    {
                        var route = currentTrip.Route ?? activeShift.Route ?? driver.CurrentRoute;
                        var boardingName = route?.Nodes?.FirstOrDefault(n =>
                            n.Id.Equals(pj.BoardingNode, StringComparison.OrdinalIgnoreCase) ||
                            n.Name.Equals(pj.BoardingNode, StringComparison.OrdinalIgnoreCase))?.Name ?? pj.BoardingNode;
                        var destinationName = route?.Nodes?.FirstOrDefault(n =>
                            n.Id.Equals(pj.DestinationNode, StringComparison.OrdinalIgnoreCase) ||
                            n.Name.Equals(pj.DestinationNode, StringComparison.OrdinalIgnoreCase))?.Name ?? pj.DestinationNode;

                        return new ActivePassengerDto
                        {
                            JourneyId = pj.Id,
                            PassengerId = pj.PassengerId,
                            PassengerName = pj.Passenger != null
                                ? $"{pj.Passenger.FirstName} {pj.Passenger.LastName}"
                                : "Commuter",
                            PassengerPhone = pj.Passenger?.Phone ?? "N/A",
                            BoardingNode = boardingName,
                            DestinationNode = destinationName,
                            Fare = pj.Fare,
                            PaymentMethod = pj.PaymentMethod.ToString(),
                            PaymentStatus = pj.PaymentStatus.ToString(),
                            AlightingStatus = pj.AlightingStatus.ToString(),
                            BoardedAt = pj.CreatedAt
                        };
                    }).ToList();

                activePassengerCount = activePassengers.Count;

                var tripRoute = currentTrip.Route ?? activeShift.Route ?? driver.CurrentRoute;
                var directionDisplay = FormatDirectionDisplay(currentTrip.Direction, tripRoute);

                activeTripDto = new ActiveTripDto
                {
                    TripId = currentTrip.Id,
                    TripNumber = currentTrip.TripNumber,
                    RouteName = tripRoute?.Name ?? "Assigned Corridor",
                    RouteCode = tripRoute?.RouteCode ?? "N/A",
                    Direction = currentTrip.Direction,
                    DirectionDisplay = directionDisplay,
                    TripStart = currentTrip.TripStart,
                    Status = currentTrip.Status.ToString(),
                    PassengersCount = activePassengers.Count,
                    TripRevenue = currentTrip.TripRevenue
                };
            }
            else
            {
                var lastCompleted = activeShift.Trips
                    .Where(t => t.Status == TripStatus.Completed)
                    .OrderByDescending(t => t.TripEnd ?? t.UpdatedAt ?? t.CreatedAt)
                    .FirstOrDefault();

                if (lastCompleted != null)
                {
                    var compRoute = lastCompleted.Route ?? activeShift.Route ?? driver.CurrentRoute;
                    lastCompletedTripDto = new CompletedTripDto
                    {
                        TripId = lastCompleted.Id,
                        TripNumber = lastCompleted.TripNumber,
                        RouteName = compRoute?.Name ?? "Assigned Corridor",
                        Direction = FormatDirectionDisplay(lastCompleted.Direction, compRoute),
                        TripStart = lastCompleted.TripStart,
                        TripEnd = lastCompleted.TripEnd,
                        Status = lastCompleted.Status.ToString(),
                        PassengersCount = lastCompleted.PassengersCount,
                        TripRevenue = lastCompleted.TripRevenue
                    };
                }
            }
        }

        var assignedRoute = driver.CurrentRoute ?? activeShift?.Route;
        var routeNodesList = new List<NodeDto>();
        var availableDirectionsList = new List<RouteDirectionDto>();

        if (assignedRoute?.Nodes != null && assignedRoute.Nodes.Count > 0)
        {
            routeNodesList = assignedRoute.Nodes.Select(n => new NodeDto
            {
                Id = n.Id,
                Name = n.Name,
                Latitude = n.Latitude,
                Longitude = n.Longitude
            }).ToList();

            if (assignedRoute.Nodes.Count >= 2)
            {
                var firstNode = assignedRoute.Nodes[0];
                var lastNode = assignedRoute.Nodes[^1];

                availableDirectionsList.Add(new RouteDirectionDto
                {
                    DirectionKey = $"{firstNode.Id}→{lastNode.Id}",
                    DisplayLabel = $"Outbound: {firstNode.Name} ({firstNode.Id}) → {lastNode.Name} ({lastNode.Id})",
                    FromNodeId = firstNode.Id,
                    FromNodeName = firstNode.Name,
                    ToNodeId = lastNode.Id,
                    ToNodeName = lastNode.Name,
                    IsForward = true
                });

                availableDirectionsList.Add(new RouteDirectionDto
                {
                    DirectionKey = $"{lastNode.Id}→{firstNode.Id}",
                    DisplayLabel = $"Inbound: {lastNode.Name} ({lastNode.Id}) → {firstNode.Name} ({firstNode.Id})",
                    FromNodeId = lastNode.Id,
                    FromNodeName = lastNode.Name,
                    ToNodeId = firstNode.Id,
                    ToNodeName = firstNode.Name,
                    IsForward = false
                });
            }
        }

        var todaySummary = await GetTodaySummaryAsync(driverId, cancellationToken);

        var response = new DriverDashboardResponse
        {
            DriverId = driver.DriverId ?? string.Empty,
            FullName = $"{driver.FirstName} {driver.LastName}",
            RouteName = assignedRoute?.Name ?? "No Route Assigned",
            RouteCode = assignedRoute?.RouteCode ?? "N/A",

            ActiveShiftId = activeShiftId,
            ActiveTripId = activeTripId,
            IsTripActive = isTripActive,
            ShiftStart = shiftStart,
            CurrentTripNumber = currentTripNumber,

            TotalRevenue = todaySummary.TotalRevenue > 0 ? todaySummary.TotalRevenue : shiftRevenue,
            TotalPassengers = todaySummary.TotalPassengers > 0 ? todaySummary.TotalPassengers : shiftPassengers,
            AvailableSeats = Math.Max(0, 15 - activePassengerCount),
            Capacity = 15,

            ActivePassengers = activePassengers,
            ActiveTrip = activeTripDto,
            LastCompletedTrip = lastCompletedTripDto,
            RouteNodes = routeNodesList,
            AvailableDirections = availableDirectionsList,
            TodaySummary = todaySummary
        };

        return Result<DriverDashboardResponse, DriverError>.Success(response);
    }

    public async Task<Result<ShiftSummaryResponse, DriverError>> GetShiftSummaryAsync(
        Guid shiftId,
        CancellationToken cancellationToken = default)
    {
        var shift = await _dbContext.DriverShifts
            .Include(s => s.Driver)
            .Include(s => s.Route)
            .Include(s => s.Trips)
                .ThenInclude(t => t.PassengerJourneys)
            .Include(s => s.Trips)
                .ThenInclude(t => t.Route)
            .FirstOrDefaultAsync(s => s.Id == shiftId && !s.IsDeleted, cancellationToken);

        if (shift == null)
            return Result<ShiftSummaryResponse, DriverError>.Failure(
                DriverError.ShiftNotFoundForSummary(shiftId));

        if (_currentUserService != null && _currentUserService.IsAuthenticated && !_currentUserService.IsAdmin && _currentUserService.DriverGuid.HasValue && shift.DriverId != _currentUserService.DriverGuid.Value)
        {
            return Result<ShiftSummaryResponse, DriverError>.Failure(DriverError.Forbidden("You do not have permission to view another driver's shift summary."));
        }

        var allJourneys = shift.Trips
            .SelectMany(t => t.PassengerJourneys)
            .Where(pj => pj.PaymentStatus == PaymentStatus.Completed)
            .ToList();

        var telebirrRevenue = allJourneys
            .Where(j => j.PaymentMethod == PaymentMethod.Telebirr)
            .Sum(j => j.Fare);

        var walletRevenue = allJourneys
            .Where(j => j.PaymentMethod == PaymentMethod.Wallet)
            .Sum(j => j.Fare);

        var cashRevenue = allJourneys
            .Where(j => j.PaymentMethod == PaymentMethod.Cash)
            .Sum(j => j.Fare);

        var digitalRevenue = telebirrRevenue + walletRevenue;

        var tripSummaries = shift.Trips
            .Where(t => t.Status == TripStatus.Completed)
            .OrderBy(t => t.TripNumber)
            .Select(t => new TripSummaryDto
            {
                TripId = t.Id,
                TripNumber = t.TripNumber,
                Direction = FormatDirectionDisplay(t.Direction, t.Route ?? shift.Route),
                TripStart = t.TripStart,
                TripEnd = t.TripEnd,
                PassengersCount = t.PassengersCount,
                TripRevenue = t.TripRevenue,
                Status = t.Status.ToString()
            })
            .ToList();

        var totalTrips = shift.Trips.Count(t => t.Status == TripStatus.Completed);
        var averageFare = shift.TotalPassengers > 0
            ? shift.TotalRevenue / shift.TotalPassengers
            : 0;

        var response = new ShiftSummaryResponse
        {
            ShiftId = shift.Id,
            ShiftStart = shift.ShiftStart,
            ShiftEnd = shift.ShiftEnd,
            Status = shift.Status.ToString(),
            TotalTrips = totalTrips,
            TotalPassengers = shift.TotalPassengers,
            TotalRevenue = shift.TotalRevenue,
            AverageFare = averageFare,
            PaymentBreakdown = new PaymentBreakdownDto
            {
                Digital = digitalRevenue,
                Cash = cashRevenue,
                Telebirr = telebirrRevenue,
                Wallet = walletRevenue
            },
            Trips = tripSummaries
        };

        return Result<ShiftSummaryResponse, DriverError>.Success(response);
    }

    public async Task<DailySummaryDto> GetTodaySummaryAsync(
        Guid driverId,
        CancellationToken cancellationToken = default)
    {
        var today = DateTime.UtcNow.Date;

        var shiftsToday = await _dbContext.DriverShifts
            .Include(s => s.Trips)
                .ThenInclude(t => t.PassengerJourneys)
            .Where(s => s.DriverId == driverId &&
                        s.ShiftStart.Date == today &&
                        !s.IsDeleted)
            .ToListAsync(cancellationToken);

        var totalTrips = shiftsToday.Sum(s => s.Trips.Count(t => t.Status == TripStatus.Completed));
        var totalPassengers = shiftsToday.Sum(s => s.TotalPassengers);
        var totalRevenue = shiftsToday.Sum(s => s.TotalRevenue);

        var allJourneys = shiftsToday
            .SelectMany(s => s.Trips)
            .SelectMany(t => t.PassengerJourneys)
            .Where(pj => pj.PaymentStatus == PaymentStatus.Completed)
            .ToList();

        var telebirrRevenue = allJourneys
            .Where(j => j.PaymentMethod == PaymentMethod.Telebirr)
            .Sum(j => j.Fare);

        var walletRevenue = allJourneys
            .Where(j => j.PaymentMethod == PaymentMethod.Wallet)
            .Sum(j => j.Fare);

        var cashRevenue = allJourneys
            .Where(j => j.PaymentMethod == PaymentMethod.Cash)
            .Sum(j => j.Fare);

        var digitalRevenue = telebirrRevenue + walletRevenue;

        var averageFare = totalPassengers > 0 ? totalRevenue / totalPassengers : 0;

        return new DailySummaryDto
        {
            TotalTrips = totalTrips,
            TotalPassengers = totalPassengers,
            TotalRevenue = totalRevenue,
            AverageFare = averageFare,
            PaymentBreakdown = new PaymentBreakdownDto
            {
                Digital = digitalRevenue,
                Cash = cashRevenue,
                Telebirr = telebirrRevenue,
                Wallet = walletRevenue
            }
        };
    }

    // TRIP MANAGEMENT
    public async Task<Result<TripResponse, DriverError>> StartNewTripAsync(
        Guid shiftId,
        string direction,
        CancellationToken cancellationToken = default)
    {
        using var activity = TaxiPayDiagnostics.ActivitySource.StartActivity("Driver.StartTrip");
        activity?.SetTag("shift.id", shiftId.ToString());
        activity?.SetTag("direction", direction);

        var shift = await _dbContext.DriverShifts
            .Include(s => s.Driver)
            .Include(s => s.Route)
            .Include(s => s.Trips)
            .FirstOrDefaultAsync(s => s.Id == shiftId && !s.IsDeleted, cancellationToken);

        if (shift == null)
            return Result<TripResponse, DriverError>.Failure(
                DriverError.ShiftNotFoundForTrip(shiftId));

        if (_currentUserService != null && _currentUserService.IsAuthenticated && !_currentUserService.IsAdmin && _currentUserService.DriverGuid.HasValue && shift.DriverId != _currentUserService.DriverGuid.Value)
        {
            return Result<TripResponse, DriverError>.Failure(DriverError.Forbidden("You cannot start a trip for another driver."));
        }

        if (shift.Status != ShiftStatus.Active)
            return Result<TripResponse, DriverError>.Failure(
                DriverError.ShiftNotActive());

        // Check for any active trip by this driver
        var existingActiveTrip = await _dbContext.Trips
            .FirstOrDefaultAsync(t => t.DriverId == shift.DriverId &&
                                      t.Status == TripStatus.Active, cancellationToken);

        if (existingActiveTrip != null)
            return Result<TripResponse, DriverError>.Failure(
                DriverError.TripAlreadyActive());

        if (shift.Route == null)
            return Result<TripResponse, DriverError>.Failure(
                DriverError.RouteNotFoundForTrip());

        var routeNodes = shift.Route.Nodes ?? new List<Node>();
        var cleanDirection = (direction ?? string.Empty).Trim();

        if (cleanDirection.Equals("FORWARD", StringComparison.OrdinalIgnoreCase) ||
            cleanDirection.Equals("Outbound", StringComparison.OrdinalIgnoreCase))
        {
            if (routeNodes.Count >= 2)
            {
                cleanDirection = $"{routeNodes[0].Id}→{routeNodes[^1].Id}";
            }
        }
        else if (cleanDirection.Equals("REVERSE", StringComparison.OrdinalIgnoreCase) ||
                 cleanDirection.Equals("Inbound", StringComparison.OrdinalIgnoreCase))
        {
            if (routeNodes.Count >= 2)
            {
                cleanDirection = $"{routeNodes[^1].Id}→{routeNodes[0].Id}";
            }
        }
        else if (cleanDirection.Contains('→'))
        {
            var parts = cleanDirection.Split('→');
            var from = parts[0].Trim();
            var to = parts.Length > 1 ? parts[1].Trim() : from;

            var fromNode = routeNodes.FirstOrDefault(n =>
                n.Id.Equals(from, StringComparison.OrdinalIgnoreCase) ||
                n.Name.Equals(from, StringComparison.OrdinalIgnoreCase));
            var toNode = routeNodes.FirstOrDefault(n =>
                n.Id.Equals(to, StringComparison.OrdinalIgnoreCase) ||
                n.Name.Equals(to, StringComparison.OrdinalIgnoreCase));

            if (fromNode != null && toNode != null)
            {
                cleanDirection = $"{fromNode.Id}→{toNode.Id}";
            }
            else
            {
                return Result<TripResponse, DriverError>.Failure(
                    DriverError.InvalidDirection($"Nodes '{from}' or '{to}' not found on assigned route."));
            }
        }
        else
        {
            if (routeNodes.Count >= 2)
            {
                cleanDirection = $"{routeNodes[0].Id}→{routeNodes[^1].Id}";
            }
            else
            {
                return Result<TripResponse, DriverError>.Failure(
                    DriverError.InvalidDirection(direction));
            }
        }

        shift.CurrentTripNumber++;

        var trip = new Trip
        {
            DriverShiftId = shift.Id,
            DriverId = shift.DriverId,
            RouteId = shift.RouteId,
            Direction = cleanDirection,
            TripNumber = shift.CurrentTripNumber,
            TripStart = DateTime.UtcNow,
            HasReachedFinalDestination = false,
            PassengersCount = 0,
            TripRevenue = 0,
            Status = TripStatus.Active,
            CreatedAt = DateTime.UtcNow
        };

        shift.CurrentTripId = trip.Id;
        shift.UpdatedAt = DateTime.UtcNow;

        _dbContext.Trips.Add(trip);
        await _dbContext.SaveChangesAsync(cancellationToken);

        TaxiPayDiagnostics.TripsTotal.Add(1, new KeyValuePair<string, object?>("action", "start"));

        await _realtimeNotifier.NotifyTripStatusUpdatedAsync(
            trip.Id,
            new List<Guid>(),
            shift.Driver?.DriverId,
            new TripStatusUpdateDto(trip.Id, trip.Status.ToString(), trip.Direction, trip.TripStart, trip.TripEnd),
            cancellationToken);

        var response = new TripResponse
        {
            TripId = trip.Id,
            ShiftId = shift.Id,
            DriverId = shift.DriverId,
            DriverName = shift.Driver != null
                ? $"{shift.Driver.FirstName} {shift.Driver.LastName}"
                : "Captain",
            TripNumber = trip.TripNumber,
            Direction = FormatDirectionDisplay(trip.Direction, shift.Route),
            RouteName = shift.Route?.Name ?? "Assigned Route",
            TripStart = trip.TripStart,
            TripEnd = trip.TripEnd,
            PassengersCount = trip.PassengersCount,
            TripRevenue = trip.TripRevenue,
            Status = trip.Status.ToString()
        };

        return Result<TripResponse, DriverError>.Success(response);
    }

    public async Task<Result<TripResponse, DriverError>> CompleteCurrentTripAsync(
        Guid shiftId,
        decimal driverLatitude,
        decimal driverLongitude,
        CancellationToken cancellationToken = default)
    {
        using var activity = TaxiPayDiagnostics.ActivitySource.StartActivity("Driver.CompleteTrip");
        activity?.SetTag("shift.id", shiftId.ToString());
        var shift = await _dbContext.DriverShifts
            .Include(s => s.Driver)
            .Include(s => s.Route)
            .Include(s => s.Trips)
                .ThenInclude(t => t.PassengerJourneys)
            .FirstOrDefaultAsync(s => s.Id == shiftId && !s.IsDeleted, cancellationToken);

        if (shift == null)
            return Result<TripResponse, DriverError>.Failure(
                DriverError.ShiftNotFoundForTrip(shiftId));

        if (_currentUserService != null && _currentUserService.IsAuthenticated && !_currentUserService.IsAdmin && _currentUserService.DriverGuid.HasValue && shift.DriverId != _currentUserService.DriverGuid.Value)
        {
            return Result<TripResponse, DriverError>.Failure(DriverError.Forbidden("You cannot complete a trip belonging to another driver."));
        }

        if (shift.Status != ShiftStatus.Active)
            return Result<TripResponse, DriverError>.Failure(
                DriverError.ShiftNotActive());

        var currentTrip = shift.Trips
            .FirstOrDefault(t => (shift.CurrentTripId.HasValue && t.Id == shift.CurrentTripId.Value) ||
                                 t.Status == TripStatus.Active);

        if (currentTrip == null || currentTrip.Status != TripStatus.Active)
            return Result<TripResponse, DriverError>.Failure(
                DriverError.NoActiveTrip());

        var activePassengersCount = currentTrip.PassengerJourneys
            .Count(pj => pj.PaymentStatus == PaymentStatus.Completed &&
                         pj.AlightingStatus != AlightingStatus.Alighted);

        // Strict Business Rule: A trip can only end when all passengers have alighted.
        if (activePassengersCount > 0)
        {
            return Result<TripResponse, DriverError>.Failure(
                DriverError.CannotCompleteTripWithPassengers(activePassengersCount));
        }

        currentTrip.HasReachedFinalDestination = true;
        currentTrip.TripEnd = DateTime.UtcNow;
        currentTrip.Status = TripStatus.Completed;
        currentTrip.UpdatedAt = DateTime.UtcNow;

        shift.TotalPassengers += currentTrip.PassengersCount;
        shift.TotalRevenue += currentTrip.TripRevenue;

        shift.CurrentTripId = null;
        shift.UpdatedAt = DateTime.UtcNow;

        var driver = await _dbContext.Drivers
            .FirstOrDefaultAsync(d => d.Id == shift.DriverId && !d.IsDeleted, cancellationToken);

        if (driver != null)
        {
            driver.TotalTrips += 1;
            driver.TotalRevenue += currentTrip.TripRevenue;
            driver.UpdatedAt = DateTime.UtcNow;
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        TaxiPayDiagnostics.TripsTotal.Add(1, new KeyValuePair<string, object?>("action", "complete"));

        var passengerIds = currentTrip.PassengerJourneys.Select(pj => pj.PassengerId).Distinct().ToList();
        await _realtimeNotifier.NotifyTripStatusUpdatedAsync(
            currentTrip.Id,
            passengerIds,
            shift.Driver?.DriverId,
            new TripStatusUpdateDto(currentTrip.Id, currentTrip.Status.ToString(), currentTrip.Direction, currentTrip.TripStart, currentTrip.TripEnd),
            cancellationToken);

        var response = new TripResponse
        {
            TripId = currentTrip.Id,
            ShiftId = shift.Id,
            DriverId = shift.DriverId,
            DriverName = shift.Driver != null
                ? $"{shift.Driver.FirstName} {shift.Driver.LastName}"
                : "Unknown",
            TripNumber = currentTrip.TripNumber,
            Direction = FormatDirectionDisplay(currentTrip.Direction, shift.Route),
            RouteName = shift.Route?.Name ?? "Unknown",
            TripStart = currentTrip.TripStart,
            TripEnd = currentTrip.TripEnd,
            PassengersCount = currentTrip.PassengersCount,
            TripRevenue = currentTrip.TripRevenue,
            Status = currentTrip.Status.ToString()
        };

        return Result<TripResponse, DriverError>.Success(response);
    }

    public async Task<Result<ActivePassengerDto, DriverError>> RequestPassengerAlightingAsync(
        Guid journeyId,
        CancellationToken cancellationToken = default)
    {
        using var activity = TaxiPayDiagnostics.ActivitySource.StartActivity("Driver.RequestAlight");
        var journey = await _dbContext.PassengerJourneyss
            .Include(pj => pj.Passenger)
            .Include(pj => pj.Trip)
                .ThenInclude(t => t.Route)
            .Include(pj => pj.Trip)
                .ThenInclude(t => t.Driver)
            .FirstOrDefaultAsync(pj => pj.Id == journeyId, cancellationToken);

        if (journey == null)
            return Result<ActivePassengerDto, DriverError>.Failure(
                DriverError.JourneyNotFound(journeyId));

        if (_currentUserService != null && _currentUserService.IsAuthenticated && !_currentUserService.IsAdmin && _currentUserService.DriverGuid.HasValue && journey.Trip?.DriverId != _currentUserService.DriverGuid.Value)
        {
            return Result<ActivePassengerDto, DriverError>.Failure(DriverError.Forbidden("You cannot manage passenger alighting for another driver's trip."));
        }

        journey.AlightingStatus = AlightingStatus.AwaitingConfirmation;
        journey.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var boardingName = journey.Trip?.Route?.Nodes?.FirstOrDefault(n =>
            n.Id.Equals(journey.BoardingNode, StringComparison.OrdinalIgnoreCase) ||
            n.Name.Equals(journey.BoardingNode, StringComparison.OrdinalIgnoreCase))?.Name ?? journey.BoardingNode;
        var destinationName = journey.Trip?.Route?.Nodes?.FirstOrDefault(n =>
            n.Id.Equals(journey.DestinationNode, StringComparison.OrdinalIgnoreCase) ||
            n.Name.Equals(journey.DestinationNode, StringComparison.OrdinalIgnoreCase))?.Name ?? journey.DestinationNode;

        var dto = new ActivePassengerDto
        {
            JourneyId = journey.Id,
            PassengerId = journey.PassengerId,
            PassengerName = journey.Passenger != null
                ? $"{journey.Passenger.FirstName} {journey.Passenger.LastName}"
                : "Unknown",
            PassengerPhone = journey.Passenger?.Phone ?? "Unknown",
            BoardingNode = boardingName,
            DestinationNode = destinationName,
            Fare = journey.Fare,
            PaymentMethod = journey.PaymentMethod.ToString(),
            PaymentStatus = journey.PaymentStatus.ToString(),
            AlightingStatus = journey.AlightingStatus.ToString(),
            BoardedAt = journey.CreatedAt
        };

        await _realtimeNotifier.NotifyAlightingRequestedAsync(
            journey.Trip?.Driver?.DriverId ?? string.Empty,
            journey.Trip?.DriverId ?? Guid.Empty,
            journey.PassengerId,
            dto,
            cancellationToken);

        // Targeted Persistent Notification for the affected passenger
        await _notificationService.CreateNotificationAsync(
            userId: journey.PassengerId,
            role: "Passenger",
            title: "Alighting Confirmation Requested",
            message: $"Your driver marked you as alighted at {destinationName}. Please confirm your alighting.",
            type: "warning",
            targetUrl: "/passenger/dashboard",
            ct: cancellationToken);

        return Result<ActivePassengerDto, DriverError>.Success(dto);
    }

    public async Task<Result<ActivePassengerDto, DriverError>> ConfirmPassengerAlightingAsync(
        Guid journeyId,
        CancellationToken cancellationToken = default)
    {
        using var activity = TaxiPayDiagnostics.ActivitySource.StartActivity("Driver.ConfirmAlighting");
        var journey = await _dbContext.PassengerJourneyss
            .Include(pj => pj.Passenger)
            .Include(pj => pj.Trip)
                .ThenInclude(t => t.Route)
            .Include(pj => pj.Trip)
                .ThenInclude(t => t.Driver)
            .FirstOrDefaultAsync(pj => pj.Id == journeyId, cancellationToken);

        if (journey == null)
            return Result<ActivePassengerDto, DriverError>.Failure(
                DriverError.JourneyNotFound(journeyId));

        if (_currentUserService != null && _currentUserService.IsAuthenticated && !_currentUserService.IsAdmin && _currentUserService.DriverGuid.HasValue && journey.Trip?.DriverId != _currentUserService.DriverGuid.Value)
        {
            return Result<ActivePassengerDto, DriverError>.Failure(DriverError.Forbidden("You cannot manage passenger alighting for another driver's trip."));
        }

        journey.AlightingStatus = AlightingStatus.Alighted;
        journey.AlightedAt = DateTime.UtcNow;
        journey.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        TaxiPayDiagnostics.TripsTotal.Add(1, new KeyValuePair<string, object?>("action", "alight"));

        var boardingName = journey.Trip?.Route?.Nodes?.FirstOrDefault(n =>
            n.Id.Equals(journey.BoardingNode, StringComparison.OrdinalIgnoreCase) ||
            n.Name.Equals(journey.BoardingNode, StringComparison.OrdinalIgnoreCase))?.Name ?? journey.BoardingNode;
        var destinationName = journey.Trip?.Route?.Nodes?.FirstOrDefault(n =>
            n.Id.Equals(journey.DestinationNode, StringComparison.OrdinalIgnoreCase) ||
            n.Name.Equals(journey.DestinationNode, StringComparison.OrdinalIgnoreCase))?.Name ?? journey.DestinationNode;

        var dto = new ActivePassengerDto
        {
            JourneyId = journey.Id,
            PassengerId = journey.PassengerId,
            PassengerName = journey.Passenger != null
                ? $"{journey.Passenger.FirstName} {journey.Passenger.LastName}"
                : "Unknown",
            PassengerPhone = journey.Passenger?.Phone ?? "Unknown",
            BoardingNode = boardingName,
            DestinationNode = destinationName,
            Fare = journey.Fare,
            PaymentMethod = journey.PaymentMethod.ToString(),
            PaymentStatus = journey.PaymentStatus.ToString(),
            AlightingStatus = journey.AlightingStatus.ToString(),
            BoardedAt = journey.CreatedAt
        };

        await _realtimeNotifier.NotifyAlightingConfirmedAsync(
            journey.Trip?.Driver?.DriverId ?? string.Empty,
            journey.Trip?.DriverId ?? Guid.Empty,
            journey.PassengerId,
            dto,
            cancellationToken);

        return Result<ActivePassengerDto, DriverError>.Success(dto);
    }

    public static string FormatDirectionDisplay(string? direction, Domain.Entities.Route? route)
    {
        if (string.IsNullOrWhiteSpace(direction))
            return route != null && route.Nodes != null && route.Nodes.Count >= 2
                ? $"{route.Nodes[0].Name} → {route.Nodes[^1].Name}"
                : "Corridor Trip";

        var dirStr = direction.Trim();

        if (route?.Nodes != null && route.Nodes.Count >= 2)
        {
            if (dirStr.Equals("FORWARD", StringComparison.OrdinalIgnoreCase) || dirStr.Equals("Outbound", StringComparison.OrdinalIgnoreCase))
                return $"{route.Nodes[0].Name} → {route.Nodes[^1].Name}";
            if (dirStr.Equals("REVERSE", StringComparison.OrdinalIgnoreCase) || dirStr.Equals("Inbound", StringComparison.OrdinalIgnoreCase))
                return $"{route.Nodes[^1].Name} → {route.Nodes[0].Name}";
        }

        var parts = dirStr.Split(new[] { '→', '-' }, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
            return dirStr;

        var fromPart = parts[0].Trim();
        var toPart = parts.Length > 1 ? parts[^1].Trim() : fromPart;

        var nodes = route?.Nodes;
        var fromName = nodes?.FirstOrDefault(n =>
            n.Id.Equals(fromPart, StringComparison.OrdinalIgnoreCase) ||
            n.Name.Equals(fromPart, StringComparison.OrdinalIgnoreCase))?.Name ?? fromPart;

        var toName = nodes?.FirstOrDefault(n =>
            n.Id.Equals(toPart, StringComparison.OrdinalIgnoreCase) ||
            n.Name.Equals(toPart, StringComparison.OrdinalIgnoreCase))?.Name ?? toPart;

        if (!string.IsNullOrEmpty(fromName) && !string.IsNullOrEmpty(toName) && fromName != toName)
        {
            return $"{fromName} → {toName}";
        }

        return fromName;
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371;

        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);

        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        return R * c;
    }

    private double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }
}

