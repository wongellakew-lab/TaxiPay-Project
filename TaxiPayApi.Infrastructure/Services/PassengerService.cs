using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.Common.Telemetry;
using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Application.DTOs.Passengers;
using TaxiPayApi.Domain.Entities;
using TaxiPayApi.Application.Interfaces;
using TaxiPayApi.Infrastructure.Persistence.Context;

namespace TaxiPayApi.Application.Services;

public class PassengerService(
        TaxiPayDbContext _dbContext,
        UserManager<TaxiPayUser> _userManager,
        IMapper _mapper,
        IOptions<JwtSettings> _jwtSettings,
        ITaxiPayRealtimeNotifier _realtimeNotifier,
        INotificationService _notificationService,
        ILogger<PassengerService> _logger,
        ICurrentUserService? _currentUserService = null) : IPassengerService
{
    private readonly JwtSettings _jwtSettings = _jwtSettings.Value;

    public async Task<Result<PassengerResponse, PassengerError>> GetProfileAsync(
        Guid passengerId,
        CancellationToken cancellationToken = default)
    {
        if (_currentUserService != null && _currentUserService.IsAuthenticated && !_currentUserService.IsAdmin && _currentUserService.PassengerGuid.HasValue && _currentUserService.PassengerGuid.Value != passengerId)
        {
            return Result<PassengerResponse, PassengerError>.Failure(
                PassengerError.Forbidden("You do not have permission to access another passenger's profile."));
        }

        var passenger = await _dbContext.Passengers
            .FirstOrDefaultAsync(p => p.Id == passengerId, cancellationToken);

        if (passenger == null)
            return Result<PassengerResponse, PassengerError>.Failure(
                PassengerError.PassengerNotFound(passengerId));

        var response = _mapper.Map<PassengerResponse>(passenger);
        return Result<PassengerResponse, PassengerError>.Success(response);
    }

    public async Task<Result<PassengerResponse, PassengerError>> UpdateProfileAsync(
        Guid passengerId,
        UpdatePassengerProfileRequest request,
        CancellationToken cancellationToken = default)
    {
        if (_currentUserService != null && _currentUserService.IsAuthenticated && !_currentUserService.IsAdmin && _currentUserService.PassengerGuid.HasValue && _currentUserService.PassengerGuid.Value != passengerId)
        {
            return Result<PassengerResponse, PassengerError>.Failure(
                PassengerError.Forbidden("You do not have permission to update another passenger's profile."));
        }

        var passenger = await _dbContext.Passengers
            .FirstOrDefaultAsync(p => p.Id == passengerId, cancellationToken);

        if (passenger == null)
            return Result<PassengerResponse, PassengerError>.Failure(
                PassengerError.PassengerNotFound(passengerId));

        var cleanPhone = request.Phone.Trim();
        if (cleanPhone != passenger.Phone)
        {
            var phoneInUse = await _dbContext.Passengers
                .AnyAsync(p => p.Phone == cleanPhone && p.Id != passengerId, cancellationToken)
                || await _userManager.Users.AnyAsync(u => u.PhoneNumber == cleanPhone && u.Id != passenger.UserId, cancellationToken);

            if (phoneInUse)
                return Result<PassengerResponse, PassengerError>.Failure(
                    PassengerError.PhoneAlreadyInUse());
        }

        passenger.FirstName = request.FirstName.Trim();
        passenger.LastName = request.LastName.Trim();
        passenger.Phone = cleanPhone;
        passenger.UpdatedAt = DateTime.UtcNow;

        if (!string.IsNullOrEmpty(passenger.UserId))
        {
            var user = await _userManager.FindByIdAsync(passenger.UserId);
            if (user != null)
            {
                user.FirstName = passenger.FirstName;
                user.LastName = passenger.LastName;
                user.PhoneNumber = cleanPhone;
                user.UserName = cleanPhone;
                user.UpdatedAt = DateTime.UtcNow;
                await _userManager.UpdateAsync(user);
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = _mapper.Map<PassengerResponse>(passenger);
        return Result<PassengerResponse, PassengerError>.Success(response);
    }

    public async Task<Result<PassengerDashboardResponse, PassengerError>> GetDashboardAsync(
        Guid passengerId,
        CancellationToken cancellationToken = default)
    {
        if (_currentUserService != null && _currentUserService.IsAuthenticated && !_currentUserService.IsAdmin && _currentUserService.PassengerGuid.HasValue && _currentUserService.PassengerGuid.Value != passengerId)
        {
            return Result<PassengerDashboardResponse, PassengerError>.Failure(
                PassengerError.Forbidden("You do not have permission to access another passenger's dashboard."));
        }

        var passenger = await _dbContext.Passengers
            .FirstOrDefaultAsync(p => p.Id == passengerId, cancellationToken);

        if (passenger == null)
            return Result<PassengerDashboardResponse, PassengerError>.Failure(
                PassengerError.PassengerNotFound(passengerId));

        var rawRecentTrips = await _dbContext.PassengerJourneyss
            .Include(pj => pj.Trip)
                .ThenInclude(t => t.Driver)
            .Include(pj => pj.Trip)
                .ThenInclude(t => t.Route)
            .Where(pj => pj.PassengerId == passengerId)
            .OrderByDescending(pj => pj.CreatedAt)
            .Take(5)
            .Select(pj => new
            {
                pj.TripId,
                Direction = pj.Trip != null ? pj.Trip.Direction : string.Empty,
                RouteName = pj.Trip != null && pj.Trip.Route != null ? pj.Trip.Route.Name : string.Empty,
                DriverFirstName = pj.Trip != null && pj.Trip.Driver != null ? pj.Trip.Driver.FirstName : string.Empty,
                DriverLastName = pj.Trip != null && pj.Trip.Driver != null ? pj.Trip.Driver.LastName : string.Empty,
                DriverId = pj.Trip != null && pj.Trip.Driver != null ? pj.Trip.Driver.DriverId ?? string.Empty : string.Empty,
                pj.Fare,
                TripStart = pj.CreatedAt,
                TripStatus = pj.Trip != null ? (TripStatus?)pj.Trip.Status : null
            })
            .ToListAsync(cancellationToken);

        var recentTrips = rawRecentTrips.Select(pj => new RecentTripDto
        {
            TripId = pj.TripId,
            Direction = pj.Direction,
            RouteName = pj.RouteName,
            DriverName = !string.IsNullOrWhiteSpace(pj.DriverFirstName) ? $"{pj.DriverFirstName} {pj.DriverLastName}" : string.Empty,
            DriverId = pj.DriverId,
            Fare = pj.Fare,
            TripStart = pj.TripStart,
            Status = pj.TripStatus.HasValue ? pj.TripStatus.Value.ToString() : "Unknown"
        }).ToList();

        var now = DateTime.UtcNow;
        var startOfMonth = new DateTime(now.Year, now.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var stats = await _dbContext.PassengerJourneyss
            .Where(pj => pj.PassengerId == passengerId &&
                         pj.PaymentStatus == PaymentStatus.Completed)
            .GroupBy(pj => 1)
            .Select(g => new
            {
                TotalTrips = g.Count(),
                TotalSpent = g.Sum(pj => pj.Fare),
                AverageFare = g.Average(pj => pj.Fare),
                TripsThisMonth = g.Count(pj => pj.CreatedAt >= startOfMonth),
                SpentThisMonth = g.Where(pj => pj.CreatedAt >= startOfMonth).Sum(pj => pj.Fare)
            })
            .FirstOrDefaultAsync(cancellationToken);

        var frequentRoutesData = await _dbContext.PassengerJourneyss
            .Where(pj => pj.PassengerId == passengerId &&
                         pj.Trip != null &&
                         pj.Trip.Route != null &&
                         pj.PaymentStatus == PaymentStatus.Completed)
            .GroupBy(pj => new
            {
                RouteId = pj.Trip!.RouteId,
                RouteName = pj.Trip.Route!.Name,
                RouteCode = pj.Trip.Route.RouteCode,
                BoardingNode = pj.BoardingNode,
                DestinationNode = pj.DestinationNode
            })
            .Select(g => new
            {
                g.Key.RouteId,
                g.Key.RouteName,
                g.Key.RouteCode,
                g.Key.BoardingNode,
                g.Key.DestinationNode,
                TripCount = g.Count(),
                Fare = g.OrderByDescending(pj => pj.CreatedAt).Select(pj => pj.Fare).FirstOrDefault()
            })
            .OrderByDescending(x => x.TripCount)
            .Take(5)
            .ToListAsync(cancellationToken);

        var routeIds = frequentRoutesData.Select(x => x.RouteId).Distinct().ToList();
        var routeNodesMap = await _dbContext.Routes
            .Where(r => routeIds.Contains(r.Id))
            .ToDictionaryAsync(r => r.Id, r => r.Nodes, cancellationToken);

        var favorites = frequentRoutesData.Select(f =>
        {
            routeNodesMap.TryGetValue(f.RouteId, out var nodes);
            var fromName = nodes?.FirstOrDefault(n =>
                n.Id.Equals(f.BoardingNode, StringComparison.OrdinalIgnoreCase) ||
                n.Name.Equals(f.BoardingNode, StringComparison.OrdinalIgnoreCase))?.Name ?? f.BoardingNode;
            var toName = nodes?.FirstOrDefault(n =>
                n.Id.Equals(f.DestinationNode, StringComparison.OrdinalIgnoreCase) ||
                n.Name.Equals(f.DestinationNode, StringComparison.OrdinalIgnoreCase))?.Name ?? f.DestinationNode;

            return new FavoriteDto
            {
                Id = f.RouteId,
                Name = $"{fromName} → {toName}",
                RouteName = f.RouteName,
                RouteCode = f.RouteCode,
                FromNode = fromName,
                ToNode = toName,
                Fare = f.Fare
            };
        }).ToList();

        var activeJourneyData = await _dbContext.PassengerJourneyss
            .Include(pj => pj.Trip)
                .ThenInclude(t => t.Driver)
            .Include(pj => pj.Trip)
                .ThenInclude(t => t.Route)
            .Where(pj => pj.PassengerId == passengerId &&
                         pj.PaymentStatus == PaymentStatus.Completed &&
                         pj.AlightingStatus != AlightingStatus.Alighted &&
                         (pj.Trip == null || pj.Trip.Status == TripStatus.Active))
            .OrderByDescending(pj => pj.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        CurrentJourneyDto? currentJourney = null;
        if (activeJourneyData != null)
        {
            var nodes = activeJourneyData.Trip?.Route?.Nodes;
            var fromName = nodes?.FirstOrDefault(n =>
                n.Id.Equals(activeJourneyData.BoardingNode, StringComparison.OrdinalIgnoreCase) ||
                n.Name.Equals(activeJourneyData.BoardingNode, StringComparison.OrdinalIgnoreCase))?.Name ?? activeJourneyData.BoardingNode;
            var toName = nodes?.FirstOrDefault(n =>
                n.Id.Equals(activeJourneyData.DestinationNode, StringComparison.OrdinalIgnoreCase) ||
                n.Name.Equals(activeJourneyData.DestinationNode, StringComparison.OrdinalIgnoreCase))?.Name ?? activeJourneyData.DestinationNode;

            var driver = activeJourneyData.Trip?.Driver;
            var driverFullName = driver != null ? $"{driver.FirstName} {driver.LastName}".Trim() : "Minibus Driver";

            currentJourney = new CurrentJourneyDto
            {
                JourneyId = activeJourneyData.Id,
                TripId = activeJourneyData.TripId,
                DriverId = driver?.Id ?? Guid.Empty,
                DriverCode = driver?.DriverId ?? string.Empty,
                DriverName = driverFullName,
                DriverPhone = driver?.Phone ?? string.Empty,
                RouteName = activeJourneyData.Trip?.Route?.Name ?? "Minibus Route",
                BoardingPoint = fromName,
                AlightingPoint = toName,
                Fare = activeJourneyData.Fare,
                PaymentMethod = activeJourneyData.PaymentMethod.ToString(),
                PaymentStatus = activeJourneyData.PaymentStatus.ToString(),
                AlightingStatus = activeJourneyData.AlightingStatus.ToString(),
                TripStatus = activeJourneyData.Trip?.Status.ToString() ?? "Active",
                BoardedAt = activeJourneyData.CreatedAt
            };
        }

        var response = new PassengerDashboardResponse
        {
            Summary = new PassengerSummaryDto
            {
                FullName = $"{passenger.FirstName} {passenger.LastName}",
                Phone = passenger.Phone,
                WalletBalance = passenger.WalletBalance,
                Rating = passenger.Rating
            },
            CurrentJourney = currentJourney,
            RecentTrips = recentTrips,
            Favorites = favorites,
            Stats = new PassengerStatsDto
            {
                TotalTrips = stats?.TotalTrips ?? 0,
                TotalSpent = stats?.TotalSpent ?? 0,
                AverageFare = stats?.AverageFare ?? 0,
                TripsThisMonth = stats?.TripsThisMonth ?? 0,
                SpentThisMonth = stats?.SpentThisMonth ?? 0
            }
        };

        return Result<PassengerDashboardResponse, PassengerError>.Success(response);
    }

    public async Task<Result<TripHistoryResponse, PassengerError>> GetTripHistoryAsync(
        Guid passengerId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        if (_currentUserService != null && _currentUserService.IsAuthenticated && !_currentUserService.IsAdmin && _currentUserService.PassengerGuid.HasValue && _currentUserService.PassengerGuid.Value != passengerId)
        {
            return Result<TripHistoryResponse, PassengerError>.Failure(
                PassengerError.Forbidden("You do not have permission to access another passenger's trip history."));
        }

        var passengerExists = await _dbContext.Passengers
            .AnyAsync(p => p.Id == passengerId, cancellationToken);

        if (!passengerExists)
            return Result<TripHistoryResponse, PassengerError>.Failure(
                PassengerError.PassengerNotFound(passengerId));

        var query = _dbContext.PassengerJourneyss
            .Include(pj => pj.Trip)
                .ThenInclude(t => t.Driver)
            .Include(pj => pj.Trip)
                .ThenInclude(t => t.Route)
            .Where(pj => pj.PassengerId == passengerId);

        var totalCount = await query.CountAsync(cancellationToken);

        var rawTrips = await query
            .OrderByDescending(pj => pj.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(pj => new
            {
                pj.TripId,
                Direction = pj.Trip != null ? pj.Trip.Direction : string.Empty,
                RouteName = pj.Trip != null && pj.Trip.Route != null ? pj.Trip.Route.Name : string.Empty,
                RouteNodes = pj.Trip != null && pj.Trip.Route != null ? pj.Trip.Route.Nodes : null,
                DriverFirstName = pj.Trip != null && pj.Trip.Driver != null ? pj.Trip.Driver.FirstName : string.Empty,
                DriverLastName = pj.Trip != null && pj.Trip.Driver != null ? pj.Trip.Driver.LastName : string.Empty,
                DriverId = pj.Trip != null && pj.Trip.Driver != null ? pj.Trip.Driver.DriverId ?? string.Empty : string.Empty,
                pj.BoardingNode,
                pj.DestinationNode,
                pj.Fare,
                PaymentMethod = (PaymentMethod)pj.PaymentMethod,
                PaymentStatus = (PaymentStatus)pj.PaymentStatus,
                pj.TransactionRef,
                TripStart = pj.CreatedAt,
                TripEnd = pj.Trip != null ? pj.Trip.TripEnd : null,
                TripStatus = pj.Trip != null ? (TripStatus?)pj.Trip.Status : null
            })
            .ToListAsync(cancellationToken);

        var trips = rawTrips.Select(pj =>
        {
            var boardingName = pj.RouteNodes?.FirstOrDefault(n =>
                n.Id.Equals(pj.BoardingNode, StringComparison.OrdinalIgnoreCase) ||
                n.Name.Equals(pj.BoardingNode, StringComparison.OrdinalIgnoreCase))?.Name;
            if (string.IsNullOrWhiteSpace(boardingName))
                boardingName = pj.BoardingNode;

            var destinationName = pj.RouteNodes?.FirstOrDefault(n =>
                n.Id.Equals(pj.DestinationNode, StringComparison.OrdinalIgnoreCase) ||
                n.Name.Equals(pj.DestinationNode, StringComparison.OrdinalIgnoreCase))?.Name;
            if (string.IsNullOrWhiteSpace(destinationName))
                destinationName = pj.DestinationNode;

            return new TripHistoryItemDto
            {
                TripId = pj.TripId,
                Direction = pj.Direction,
                RouteName = pj.RouteName,
                DriverName = !string.IsNullOrWhiteSpace(pj.DriverFirstName) ? $"{pj.DriverFirstName} {pj.DriverLastName}" : string.Empty,
                DriverId = pj.DriverId,
                BoardingNode = boardingName,
                DestinationNode = destinationName,
                BoardingPointName = boardingName,
                DestinationPointName = destinationName,
                Fare = pj.Fare,
                PaymentMethod = pj.PaymentMethod.ToString(),
                PaymentStatus = pj.PaymentStatus.ToString(),
                TransactionRef = pj.TransactionRef,
                TripStart = pj.TripStart,
                TripEnd = pj.TripEnd,
                Status = pj.TripStatus.HasValue ? pj.TripStatus.Value.ToString() : "Unknown"
            };
        }).ToList();

        var totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

        var response = new TripHistoryResponse
        {
            Trips = trips,
            TotalCount = totalCount,
            PageNumber = pageNumber,
            PageSize = pageSize,
            TotalPages = totalPages,
            HasNextPage = pageNumber < totalPages,
            HasPreviousPage = pageNumber > 1
        };

        return Result<TripHistoryResponse, PassengerError>.Success(response);
    }

    public async Task<Result<DriverByCodeResponse, PassengerError>> GetDriverByCodeAsync(
        string driverId,
        CancellationToken cancellationToken = default)
    {
        var cleanCode = (driverId ?? string.Empty).Trim().ToLower();
        var normalizedCode = cleanCode.Replace("-", "").Replace(" ", "").Replace("_", "");

        var driver = await _dbContext.Drivers
            .Include(d => d.CurrentRoute)
            .FirstOrDefaultAsync(d => d.DriverId != null && (
                d.DriverId.ToLower() == cleanCode ||
                (normalizedCode.Length > 0 && d.DriverId.ToLower().Replace("-", "").Replace(" ", "").Replace("_", "") == normalizedCode)
            ), cancellationToken);

        if (driver == null)
            return Result<DriverByCodeResponse, PassengerError>.Failure(
                PassengerError.DriverNotFound(driverId));

        var activeShift = await _dbContext.DriverShifts
            .FirstOrDefaultAsync(s => s.DriverId == driver.Id &&
                                      s.Status == ShiftStatus.Active, cancellationToken);

        var hasActiveShift = activeShift != null;

        // Authoritative Database Query: Find driver's current Active trip regardless of shift collection tracking
        var activeTrip = await _dbContext.Trips
            .AsNoTracking()
            .Where(t => t.DriverId == driver.Id && t.Status == TripStatus.Active)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (activeTrip == null && activeShift?.CurrentTripId != null)
        {
            activeTrip = await _dbContext.Trips
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == activeShift.CurrentTripId.Value && t.Status == TripStatus.Active, cancellationToken);
        }

        var hasActiveTrip = activeTrip != null;
        Guid? activeTripId = activeTrip?.Id;
        string? activeTripDir = activeTrip?.Direction;
        string? activeTripDirDisplay = null;

        if (activeTrip != null && !string.IsNullOrWhiteSpace(activeTrip.Direction))
        {
            var parts = activeTrip.Direction.Split('→');
            var from = parts[0].Trim();
            var to = parts.Length > 1 ? parts[1].Trim() : from;
            var fromName = driver.CurrentRoute?.Nodes.FirstOrDefault(n => n.Id.Equals(from, StringComparison.OrdinalIgnoreCase))?.Name ?? from;
            var toName = driver.CurrentRoute?.Nodes.FirstOrDefault(n => n.Id.Equals(to, StringComparison.OrdinalIgnoreCase))?.Name ?? to;
            activeTripDirDisplay = $"{fromName} → {toName}";
        }

        var response = new DriverByCodeResponse
        {
            DriverId = driver.DriverId ?? string.Empty,
            FullName = $"{driver.FirstName} {driver.LastName}",
            Phone = driver.Phone,
            Rating = driver.Rating,
            PhotoUrl = driver.PhotoUrl,
            RouteName = driver.CurrentRoute?.Name ?? "No Route Assigned",
            RouteCode = driver.CurrentRoute?.RouteCode ?? "N/A",
            RouteNodes = driver.CurrentRoute?.Nodes.Select(n => new NodeDto
            {
                Id = n.Id,
                Name = n.Name,
                Latitude = n.Latitude,
                Longitude = n.Longitude
            }).ToList() ?? new List<NodeDto>(),
            IsActive = driver.Status == DriverStatus.Active,
            HasActiveShift = hasActiveShift,
            HasActiveTrip = hasActiveTrip,
            ActiveTripId = activeTripId,
            ActiveTripDirection = activeTripDir,
            ActiveTripDirectionDisplay = activeTripDirDisplay
        };

        return Result<DriverByCodeResponse, PassengerError>.Success(response);
    }

    public async Task<Result<List<DriverSearchResultDto>, PassengerError>> SearchDriversByCodeAsync(
        string query,
        int limit = 10,
        CancellationToken cancellationToken = default)
    {
        var cleanQuery = (query ?? string.Empty).Trim().ToLower();
        var normalizedQuery = cleanQuery.Replace("-", "").Replace(" ", "").Replace("_", "");

        var driversQuery = _dbContext.Drivers
            .Include(d => d.CurrentRoute)
            .Where(d => d.DriverId != null && d.DriverId != string.Empty);

        if (!string.IsNullOrWhiteSpace(cleanQuery))
        {
            driversQuery = driversQuery.Where(d =>
                (d.DriverId != null && (
                    d.DriverId.ToLower().Contains(cleanQuery) ||
                    (normalizedQuery.Length > 0 && d.DriverId.ToLower().Replace("-", "").Replace(" ", "").Replace("_", "").Contains(normalizedQuery))
                )) ||
                d.FirstName.ToLower().Contains(cleanQuery) ||
                d.LastName.ToLower().Contains(cleanQuery) ||
                (d.CurrentRoute != null && (
                    d.CurrentRoute.RouteCode.ToLower().Contains(cleanQuery) ||
                    d.CurrentRoute.Name.ToLower().Contains(cleanQuery) ||
                    (normalizedQuery.Length > 0 && d.CurrentRoute.RouteCode.ToLower().Replace("-", "").Contains(normalizedQuery))
                ))
            );
        }

        var drivers = await driversQuery
            .OrderBy(d => d.DriverId)
            .Take(Math.Clamp(limit, 1, 50))
            .ToListAsync(cancellationToken);

        var driverIds = drivers.Select(d => d.Id).ToList();
        var activeShiftDriverIds = await _dbContext.DriverShifts
            .Where(s => driverIds.Contains(s.DriverId) && s.Status == ShiftStatus.Active)
            .Select(s => s.DriverId)
            .ToListAsync(cancellationToken);

        var result = drivers.Select(d => new DriverSearchResultDto
        {
            DriverId = d.DriverId ?? string.Empty,
            FullName = $"{d.FirstName} {d.LastName}",
            RouteName = d.CurrentRoute?.Name ?? "No Route Assigned",
            RouteCode = d.CurrentRoute?.RouteCode ?? "N/A",
            Rating = (double)d.Rating,
            PhotoUrl = d.PhotoUrl,
            IsActive = d.Status == DriverStatus.Active,
            HasActiveShift = activeShiftDriverIds.Contains(d.Id)
        }).ToList();

        return Result<List<DriverSearchResultDto>, PassengerError>.Success(result);
    }

    public async Task<Result<FareResponse, PassengerError>> GetFareAsync(
        Guid routeId,
        string fromNode,
        string toNode,
        CancellationToken cancellationToken = default)
    {
        using var activity = TaxiPayDiagnostics.ActivitySource.StartActivity("Passenger.GetFare");
        activity?.SetTag("route.id", routeId.ToString());
        activity?.SetTag("from_node", fromNode);
        activity?.SetTag("to_node", toNode);

        var route = await _dbContext.Routes
            .FirstOrDefaultAsync(r => r.Id == routeId && r.IsActive, cancellationToken);

        if (route == null)
            return Result<FareResponse, PassengerError>.Failure(
                PassengerError.RouteNotFound(routeId));

        var nodeIds = route.Nodes.Select(n => n.Id).ToList();
        if (!nodeIds.Contains(fromNode) || !nodeIds.Contains(toNode))
            return Result<FareResponse, PassengerError>.Failure(
                PassengerError.InvalidNodes(fromNode, toNode));

        var fare = await _dbContext.FareMatrices
            .Where(f => f.RouteId == routeId &&
                        f.FromNode == fromNode &&
                        f.ToNode == toNode &&
                        f.IsActive)
            .Select(f => f.Fare)
            .FirstOrDefaultAsync(cancellationToken);

        if (fare == 0)
            return Result<FareResponse, PassengerError>.Failure(
                PassengerError.FareNotFound(fromNode, toNode));

        var response = new FareResponse
        {
            FromNode = fromNode,
            ToNode = toNode,
            Fare = fare,
            RouteName = route.Name,
            RouteCode = route.RouteCode,
            IsValidRoute = true
        };

        return Result<FareResponse, PassengerError>.Success(response);
    }

    public async Task<Result<PassengerResponse, PassengerError>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var phone = request.Phone.Trim();
        var existingUser = await _userManager.FindByNameAsync(phone)
            ?? await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone, cancellationToken);

        if (existingUser != null || await _dbContext.Passengers.AnyAsync(p => p.Phone == phone, cancellationToken))
            return Result<PassengerResponse, PassengerError>.Failure(
                PassengerError.PhoneAlreadyRegistered());

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
            var errorMsg = identityResult.Errors.FirstOrDefault()?.Description ?? "Failed to create identity account.";
            return Result<PassengerResponse, PassengerError>.Failure(PassengerError.ValidationFailed(errorMsg));
        }

        await _userManager.AddToRoleAsync(user, "Passenger");

        var passenger = new Passenger
        {
            UserId = user.Id,
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Phone = phone,
            WalletBalance = 0,
            TotalTrips = 0,
            TotalSpent = 0,
            Rating = 5.0m,
            IsVerified = true,
            VerifiedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Passengers.Add(passenger);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var response = _mapper.Map<PassengerResponse>(passenger);
        return Result<PassengerResponse, PassengerError>.Success(response);
    }

    public async Task<Result<PassengerLoginResponse, PassengerError>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var input = request.Phone.Trim();
        var user = await _userManager.FindByNameAsync(input)
            ?? (input.Contains('@') ? await _userManager.FindByEmailAsync(input) : null)
            ?? await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == input || u.Email == input, cancellationToken);

        if (user == null)
            return Result<PassengerLoginResponse, PassengerError>.Failure(
                PassengerError.InvalidCredentials());

        if (await _userManager.IsLockedOutAsync(user))
        {
            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
            var remainingMinutes = lockoutEnd.HasValue
                ? Math.Max(1, (int)Math.Ceiling((lockoutEnd.Value - DateTimeOffset.UtcNow).TotalMinutes))
                : 5;
            return Result<PassengerLoginResponse, PassengerError>.Failure(
                PassengerError.AccountLockedOut(remainingMinutes));
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
                return Result<PassengerLoginResponse, PassengerError>.Failure(
                    PassengerError.AccountLockedOut(remainingMinutes));
            }

            return Result<PassengerLoginResponse, PassengerError>.Failure(
                PassengerError.InvalidCredentials());
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var passenger = await _dbContext.Passengers
            .FirstOrDefaultAsync(p => p.UserId == user.Id || p.Phone == input || p.Phone == user.PhoneNumber, cancellationToken);

        if (passenger == null)
        {
            passenger = new Passenger
            {
                UserId = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Phone = user.PhoneNumber ?? input,
                WalletBalance = 0,
                TotalTrips = 0,
                TotalSpent = 0,
                Rating = 5.0m,
                IsVerified = true,
                VerifiedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };
            _dbContext.Passengers.Add(passenger);
        }
        else if (string.IsNullOrEmpty(passenger.UserId))
        {
            passenger.UserId = user.Id;
        }

        passenger.LastLoginAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(cancellationToken);

        var (accessToken, refreshToken, expiresAt) = GenerateTokens(passenger, user);

        var response = new PassengerLoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            TokenExpiresAt = expiresAt,
            LoginTime = DateTime.UtcNow,
            Passenger = _mapper.Map<PassengerResponse>(passenger)
        };

        return Result<PassengerLoginResponse, PassengerError>.Success(response);
    }

    public async Task<Result<PayTripResponse, PassengerError>> PayForTripAsync(
        Guid passengerId,
        PayTripRequest request,
        CancellationToken cancellationToken = default)
    {
        using var activity = TaxiPayDiagnostics.ActivitySource.StartActivity("Passenger.PayTrip");
        activity?.SetTag("payment.method", request.PaymentMethod.ToString());

        if (_currentUserService != null && _currentUserService.IsAuthenticated && !_currentUserService.IsAdmin && _currentUserService.PassengerGuid.HasValue && _currentUserService.PassengerGuid.Value != passengerId)
        {
            return Result<PayTripResponse, PassengerError>.Failure(
                PassengerError.Forbidden("You do not have permission to pay for trips using another passenger's account."));
        }

        var passenger = await _dbContext.Passengers
            .FirstOrDefaultAsync(p => p.Id == passengerId, cancellationToken);

        if (passenger == null)
            return Result<PayTripResponse, PassengerError>.Failure(
                PassengerError.PassengerNotFound(passengerId));

        var cleanDriverCode = (request.DriverId ?? string.Empty).Trim().ToLower();
        var normalizedDriverCode = cleanDriverCode.Replace("-", "").Replace(" ", "").Replace("_", "");

        var driver = await _dbContext.Drivers
            .Include(d => d.CurrentRoute)
            .FirstOrDefaultAsync(d => d.DriverId != null && (
                d.DriverId.ToLower() == cleanDriverCode ||
                (normalizedDriverCode.Length > 0 && d.DriverId.ToLower().Replace("-", "").Replace(" ", "").Replace("_", "") == normalizedDriverCode)
            ), cancellationToken);

        if (driver == null)
            return Result<PayTripResponse, PassengerError>.Failure(
                PassengerError.DriverNotFound(request.DriverId));

        if (driver.Status != DriverStatus.Active)
            return Result<PayTripResponse, PassengerError>.Failure(
                PassengerError.DriverNotActive());

        var shift = await _dbContext.DriverShifts
            .Include(s => s.Route)
            .FirstOrDefaultAsync(s => s.DriverId == driver.Id &&
                                      s.Status == ShiftStatus.Active, cancellationToken);

        if (shift == null)
            return Result<PayTripResponse, PassengerError>.Failure(
                PassengerError.DriverNotOnShift());

        var route = shift.Route;
        if (route == null)
            return Result<PayTripResponse, PassengerError>.Failure(
                PassengerError.RouteNotFound(shift.RouteId));

        var nodeIds = route.Nodes.Select(n => n.Id).ToList();
        if (!nodeIds.Contains(request.BoardingNode) || !nodeIds.Contains(request.DestinationNode))
            return Result<PayTripResponse, PassengerError>.Failure(
                PassengerError.InvalidNodes(request.BoardingNode, request.DestinationNode));

        var fare = await _dbContext.FareMatrices
            .Where(f => f.RouteId == route.Id &&
                        f.FromNode == request.BoardingNode &&
                        f.ToNode == request.DestinationNode &&
                        f.IsActive)
            .Select(f => f.Fare)
            .FirstOrDefaultAsync(cancellationToken);

        if (fare == 0)
            return Result<PayTripResponse, PassengerError>.Failure(
                PassengerError.FareNotFound(request.BoardingNode, request.DestinationNode));

        // 4. Strict Backend Verification: Driver must have an active trip currently accepting passengers
        var trip = await _dbContext.Trips
            .Where(t => t.DriverId == driver.Id && t.Status == TripStatus.Active)
            .OrderByDescending(t => t.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        if (trip == null && shift.CurrentTripId.HasValue)
        {
            trip = await _dbContext.Trips
                .FirstOrDefaultAsync(t => t.Id == shift.CurrentTripId.Value, cancellationToken);
        }

        if (trip == null)
        {
            return Result<PayTripResponse, PassengerError>.Failure(
                PassengerError.DriverNoActiveTrip());
        }

        if (trip.Status != TripStatus.Active)
        {
            return Result<PayTripResponse, PassengerError>.Failure(
                PassengerError.TripNotActive());
        }

        if (shift.CurrentTripId != trip.Id)
        {
            shift.CurrentTripId = trip.Id;
        }

        string transactionRef;
        if (request.PaymentMethod == PaymentMethod.Wallet)
        {
            if (passenger.WalletBalance < fare)
                return Result<PayTripResponse, PassengerError>.Failure(
                    PassengerError.InsufficientBalance());

            passenger.WalletBalance -= fare;
            transactionRef = $"WALLET-{Guid.NewGuid():N}";
        }
        else if (request.PaymentMethod == PaymentMethod.Telebirr)
        {
            transactionRef = $"TELE-{Guid.NewGuid():N}";
        }
        else
        {
            return Result<PayTripResponse, PassengerError>.Failure(
                PassengerError.InvalidPaymentMethod());
        }

        using var tx = await _dbContext.Database.BeginTransactionAsync(cancellationToken);

        var journey = new PassengerJourney
        {
            TripId = trip.Id,
            PassengerId = passenger.Id,
            BoardingNode = request.BoardingNode,
            DestinationNode = request.DestinationNode,
            Fare = fare,
            PaymentMethod = request.PaymentMethod,
            PaymentStatus = PaymentStatus.Completed,
            TransactionRef = transactionRef,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.PassengerJourneyss.Add(journey);

        trip.PassengersCount++;
        trip.TripRevenue += fare;
        trip.UpdatedAt = DateTime.UtcNow;

        shift.TotalPassengers++;
        shift.TotalRevenue += fare;
        shift.UpdatedAt = DateTime.UtcNow;

        passenger.TotalTrips++;
        passenger.TotalSpent += fare;
        passenger.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        var boardingName = route.Nodes?.FirstOrDefault(n =>
            n.Id.Equals(request.BoardingNode, StringComparison.OrdinalIgnoreCase) ||
            n.Name.Equals(request.BoardingNode, StringComparison.OrdinalIgnoreCase))?.Name ?? request.BoardingNode;
        var destinationName = route.Nodes?.FirstOrDefault(n =>
            n.Id.Equals(request.DestinationNode, StringComparison.OrdinalIgnoreCase) ||
            n.Name.Equals(request.DestinationNode, StringComparison.OrdinalIgnoreCase))?.Name ?? request.DestinationNode;

        var receiptNumber = $"RCP-{DateTime.UtcNow:yyyyMMdd}-{journey.Id.ToString()[..8].ToUpper()}";

        var receipt = new Receipt
        {
            TripId = trip.Id,
            PassengerId = passenger.Id,
            JourneyId = journey.Id,
            ReceiptNumber = receiptNumber,
            PassengerPhone = passenger.Phone,
            PassengerName = $"{passenger.FirstName} {passenger.LastName}",
            DriverName = $"{driver.FirstName} {driver.LastName}",
            DriverCode = driver.DriverId ?? string.Empty,
            RouteName = route.Name,
            BoardingNode = boardingName,
            DestinationNode = destinationName,
            Fare = fare,
            PaymentMethod = request.PaymentMethod.ToString(),
            PaymentStatus = PaymentStatus.Completed.ToString(),
            TransactionRef = transactionRef,
            ReceiptText = $"MINIBUS E-TICKET: {route.Name} ({boardingName} → {destinationName}) | Fare: {fare:F2} ETB | Driver: {driver.FirstName} {driver.LastName} ({driver.DriverId})",
            SentAt = DateTime.UtcNow,
            DeliveryStatus = ReceiptDeliveryStatus.Sent,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Receipts.Add(receipt);
        await _dbContext.SaveChangesAsync(cancellationToken);
        await tx.CommitAsync(cancellationToken);

        TaxiPayDiagnostics.PaymentsTotal.Add(1,
            new KeyValuePair<string, object?>("method", request.PaymentMethod.ToString()),
            new KeyValuePair<string, object?>("status", "success"));
        TaxiPayDiagnostics.PaymentsAmount.Record((double)fare,
            new KeyValuePair<string, object?>("method", request.PaymentMethod.ToString()));

        _logger.LogInformation("PaymentCompleted for PassengerId: {PassengerId}, TripId: {TripId}, Fare: {Fare}, Method: {PaymentMethod}, ReceiptNumber: {ReceiptNumber}",
            passenger.Id, trip.Id, fare, request.PaymentMethod, receiptNumber);

        var activePassengerDto = new ActivePassengerDto
        {
            JourneyId = journey.Id,
            PassengerId = passenger.Id,
            PassengerName = $"{passenger.FirstName} {passenger.LastName}",
            PassengerPhone = passenger.Phone,
            BoardingNode = boardingName,
            DestinationNode = destinationName,
            Fare = fare,
            PaymentMethod = request.PaymentMethod.ToString(),
            PaymentStatus = PaymentStatus.Completed.ToString(),
            AlightingStatus = AlightingStatus.OnBoard.ToString(),
            BoardedAt = journey.CreatedAt
        };

        // Notify the relevant driver via SignalR after the database commit succeeds
        await _realtimeNotifier.NotifyPassengerAddedToManifestAsync(
            driver.DriverId ?? string.Empty,
            driver.Id,
            activePassengerDto,
            cancellationToken);

        var response = new PayTripResponse
        {
            TripId = trip.Id,
            PassengerId = passenger.Id,
            DriverId = driver.DriverId ?? string.Empty,
            DriverName = $"{driver.FirstName} {driver.LastName}",
            RouteName = route.Name,
            BoardingNode = request.BoardingNode,
            DestinationNode = request.DestinationNode,
            Fare = fare,
            PaymentMethod = request.PaymentMethod.ToString(),
            PaymentStatus = PaymentStatus.Completed.ToString(),
            PaidAt = DateTime.UtcNow,
            ReceiptNumber = receiptNumber,
            NewBalance = passenger.WalletBalance
        };

        return Result<PayTripResponse, PassengerError>.Success(response);
    }

    public async Task<Result<TopUpWalletResponse, PassengerError>> TopUpWalletAsync(
        Guid passengerId,
        TopUpWalletRequest request,
        CancellationToken cancellationToken = default)
    {
        using var activity = TaxiPayDiagnostics.ActivitySource.StartActivity("Passenger.TopUpWallet");

        if (request.Amount <= 0)
            return Result<TopUpWalletResponse, PassengerError>.Failure(
                new PassengerError("invalid_amount", "Top-up amount must be greater than 0."));

        if (_currentUserService != null && _currentUserService.IsAuthenticated && !_currentUserService.IsAdmin && _currentUserService.PassengerGuid.HasValue && _currentUserService.PassengerGuid.Value != passengerId)
        {
            return Result<TopUpWalletResponse, PassengerError>.Failure(
                PassengerError.Forbidden("You do not have permission to top up another passenger's wallet."));
        }

        var passenger = await _dbContext.Passengers
            .FirstOrDefaultAsync(p => p.Id == passengerId, cancellationToken);

        if (passenger == null)
            return Result<TopUpWalletResponse, PassengerError>.Failure(
                PassengerError.PassengerNotFound(passengerId));

        passenger.WalletBalance += request.Amount;
        passenger.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("WalletTopUpCompleted for PassengerId: {PassengerId}, Amount: {Amount}, NewBalance: {NewBalance}",
            passenger.Id, request.Amount, passenger.WalletBalance);

        var response = new TopUpWalletResponse
        {
            PassengerId = passenger.Id,
            Amount = request.Amount,
            NewBalance = passenger.WalletBalance,
            TransactionRef = $"TOPUP-{Guid.NewGuid():N}",
            TopUpTime = DateTime.UtcNow
        };

        return Result<TopUpWalletResponse, PassengerError>.Success(response);
    }

    private Trip CreateNewTrip(DriverShift shift, string fromNode, string toNode)
    {
        shift.CurrentTripNumber++;
        var direction = $"{fromNode}→{toNode}";

        return new Trip
        {
            DriverShiftId = shift.Id,
            DriverId = shift.DriverId,
            RouteId = shift.RouteId,
            Direction = direction,
            TripNumber = shift.CurrentTripNumber,
            TripStart = DateTime.UtcNow,
            HasReachedFinalDestination = false,
            PassengersCount = 0,
            TripRevenue = 0,
            Status = TripStatus.Active,
            CreatedAt = DateTime.UtcNow
        };
    }

    private (string accessToken, string refreshToken, DateTime expiresAt) GenerateTokens(Passenger passenger, TaxiPayUser user)
    {
        var expiresAt = DateTime.UtcNow.AddHours(_jwtSettings.AccessTokenExpiryHours);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim("passengerId", passenger.Id.ToString()),
            new Claim(ClaimTypes.Name, $"{passenger.FirstName} {passenger.LastName}"),
            new Claim(ClaimTypes.MobilePhone, passenger.Phone),
            new Claim(ClaimTypes.Role, "Passenger"),
            new Claim("role", "passenger")
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

    public async Task<Result<PassengerRouteRequestDto, PassengerError>> SubmitRouteRequestAsync(
        Guid passengerId,
        CreatePassengerRouteRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (_currentUserService != null && _currentUserService.IsAuthenticated && !_currentUserService.IsAdmin && _currentUserService.PassengerGuid.HasValue && _currentUserService.PassengerGuid.Value != passengerId)
        {
            return Result<PassengerRouteRequestDto, PassengerError>.Failure(
                PassengerError.Forbidden("You do not have permission to submit route requests for another passenger."));
        }

        var passenger = await _dbContext.Passengers
            .FirstOrDefaultAsync(p => p.Id == passengerId, cancellationToken);

        if (passenger == null)
            return Result<PassengerRouteRequestDto, PassengerError>.Failure(
                PassengerError.PassengerNotFound(passengerId));

        if (string.IsNullOrWhiteSpace(request.FromLocation) || string.IsNullOrWhiteSpace(request.ToLocation))
            return Result<PassengerRouteRequestDto, PassengerError>.Failure(
                new PassengerError("invalid_request", "Boarding location and destination are required."));

        var routeReq = new PassengerRouteRequest
        {
            PassengerId = passengerId,
            FromLocation = request.FromLocation.Trim(),
            ToLocation = request.ToLocation.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            Status = PassengerRouteRequestStatus.Pending,
            RequestedAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.PassengerRouteRequests.Add(routeReq);
        await _dbContext.SaveChangesAsync(cancellationToken);

        var dto = new PassengerRouteRequestDto
        {
            Id = routeReq.Id,
            PassengerId = passengerId,
            PassengerName = $"{passenger.FirstName} {passenger.LastName}",
            PassengerPhone = passenger.Phone,
            FromLocation = routeReq.FromLocation,
            ToLocation = routeReq.ToLocation,
            Description = routeReq.Description,
            Status = routeReq.Status.ToString(),
            RequestedAt = routeReq.RequestedAt,
            ReviewedAt = routeReq.ReviewedAt,
            AdminNotes = routeReq.AdminNotes
        };

        // Notify admins via SignalR after database save succeeds
        await _realtimeNotifier.NotifyPassengerRouteRequestSubmittedAsync(dto, cancellationToken);

        return Result<PassengerRouteRequestDto, PassengerError>.Success(dto);
    }

    public async Task<Result<List<PassengerRouteRequestDto>, PassengerError>> GetPassengerRouteRequestsAsync(
        Guid passengerId,
        CancellationToken cancellationToken = default)
    {
        if (_currentUserService != null && _currentUserService.IsAuthenticated && !_currentUserService.IsAdmin && _currentUserService.PassengerGuid.HasValue && _currentUserService.PassengerGuid.Value != passengerId)
        {
            return Result<List<PassengerRouteRequestDto>, PassengerError>.Failure(
                PassengerError.Forbidden("You do not have permission to view another passenger's route requests."));
        }

        var passenger = await _dbContext.Passengers
            .FirstOrDefaultAsync(p => p.Id == passengerId, cancellationToken);

        if (passenger == null)
            return Result<List<PassengerRouteRequestDto>, PassengerError>.Failure(
                PassengerError.PassengerNotFound(passengerId));

        var requests = await _dbContext.PassengerRouteRequests
            .Where(r => r.PassengerId == passengerId)
            .OrderByDescending(r => r.RequestedAt)
            .ToListAsync(cancellationToken);

        var dtos = requests.Select(r => new PassengerRouteRequestDto
        {
            Id = r.Id,
            PassengerId = r.PassengerId,
            PassengerName = $"{passenger.FirstName} {passenger.LastName}",
            PassengerPhone = passenger.Phone,
            FromLocation = r.FromLocation,
            ToLocation = r.ToLocation,
            Description = r.Description,
            Status = r.Status.ToString(),
            RequestedAt = r.RequestedAt,
            ReviewedAt = r.ReviewedAt,
            AdminNotes = r.AdminNotes
        }).ToList();

        return Result<List<PassengerRouteRequestDto>, PassengerError>.Success(dtos);
    }

    public async Task<Result<ActivePassengerDto, PassengerError>> ConfirmPassengerAlightingAsync(
        Guid passengerId,
        Guid journeyId,
        CancellationToken cancellationToken = default)
    {
        using var activity = TaxiPayDiagnostics.ActivitySource.StartActivity("Passenger.ConfirmAlighting");
        if (_currentUserService != null && _currentUserService.IsAuthenticated && !_currentUserService.IsAdmin && _currentUserService.PassengerGuid.HasValue && _currentUserService.PassengerGuid.Value != passengerId)
        {
            return Result<ActivePassengerDto, PassengerError>.Failure(
                PassengerError.Forbidden("You do not have permission to confirm alighting for another passenger."));
        }

        var journey = await _dbContext.PassengerJourneyss
            .Include(pj => pj.Passenger)
            .Include(pj => pj.Trip)
                .ThenInclude(t => t.Route)
            .Include(pj => pj.Trip)
                .ThenInclude(t => t.Driver)
            .FirstOrDefaultAsync(pj => pj.Id == journeyId, cancellationToken);

        if (journey == null)
            return Result<ActivePassengerDto, PassengerError>.Failure(
                PassengerError.JourneyNotFound(journeyId));

        if (journey.PassengerId != passengerId)
            return Result<ActivePassengerDto, PassengerError>.Failure(
                PassengerError.UnauthorizedPassenger());

        var boardingName = journey.Trip?.Route?.Nodes?.FirstOrDefault(n =>
            n.Id.Equals(journey.BoardingNode, StringComparison.OrdinalIgnoreCase) ||
            n.Name.Equals(journey.BoardingNode, StringComparison.OrdinalIgnoreCase))?.Name ?? journey.BoardingNode;
        var destinationName = journey.Trip?.Route?.Nodes?.FirstOrDefault(n =>
            n.Id.Equals(journey.DestinationNode, StringComparison.OrdinalIgnoreCase) ||
            n.Name.Equals(journey.DestinationNode, StringComparison.OrdinalIgnoreCase))?.Name ?? journey.DestinationNode;

        if (journey.AlightingStatus == AlightingStatus.Alighted)
        {
            // Already alighted - return success idempotently
            return Result<ActivePassengerDto, PassengerError>.Success(new ActivePassengerDto
            {
                JourneyId = journey.Id,
                PassengerId = journey.PassengerId,
                PassengerName = journey.Passenger != null ? $"{journey.Passenger.FirstName} {journey.Passenger.LastName}" : "Unknown",
                PassengerPhone = journey.Passenger?.Phone ?? "Unknown",
                BoardingNode = boardingName,
                DestinationNode = destinationName,
                Fare = journey.Fare,
                PaymentMethod = journey.PaymentMethod.ToString(),
                PaymentStatus = journey.PaymentStatus.ToString(),
                AlightingStatus = journey.AlightingStatus.ToString(),
                BoardedAt = journey.CreatedAt
            });
        }

        journey.AlightingStatus = AlightingStatus.Alighted;
        journey.AlightedAt = DateTime.UtcNow;
        journey.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);

        TaxiPayDiagnostics.TripsTotal.Add(1, new KeyValuePair<string, object?>("action", "alight"));

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

        // Notify both Driver and Passenger in real time via SignalR
        await _realtimeNotifier.NotifyAlightingConfirmedAsync(
            journey.Trip?.Driver?.DriverId ?? string.Empty,
            journey.Trip?.DriverId ?? Guid.Empty,
            journey.PassengerId,
            dto,
            cancellationToken);

        if (journey.Trip != null && journey.Trip.DriverId != Guid.Empty)
        {
            await _notificationService.CreateNotificationAsync(
                userId: journey.Trip.DriverId,
                role: "Driver",
                title: "Passenger Alighted",
                message: $"{dto.PassengerName} confirmed drop-off and alighted at {destinationName}.",
                type: "success",
                targetUrl: "/driver/dashboard",
                ct: cancellationToken);
        }

        _logger.LogInformation("AlightingConfirmed for JourneyId: {JourneyId}, PassengerId: {PassengerId}, TripId: {TripId}",
            journey.Id, passengerId, journey.TripId);

        return Result<ActivePassengerDto, PassengerError>.Success(dto);
    }
}