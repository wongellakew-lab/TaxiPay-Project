using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.Common.Telemetry;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.DTOs.Notifications;
using TaxiPayApi.Application.DTOs.Realtime;
using TaxiPayApi.Application.Interfaces;
using TaxiPayApi.Domain.Entities;
using TaxiPayApi.Infrastructure.Persistence.Context;

namespace TaxiPayApi.Infrastructure.Services;

public class AdminService(
    TaxiPayDbContext _dbContext,
    UserManager<TaxiPayUser> _userManager,
    IOptions<JwtSettings> jwtSettings,
    ITaxiPayRealtimeNotifier _realtimeNotifier) : IAdminService
{
    private readonly JwtSettings _jwtSettings = jwtSettings.Value;

    public async Task<Result<AdminLoginResponse, AdminError>> LoginAsync(AdminLoginRequest request, CancellationToken ct = default)
    {
        var input = request.Email.Trim();
        var user = await _userManager.FindByEmailAsync(input)
            ?? await _userManager.FindByNameAsync(input)
            ?? await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == input || u.Email == input, ct);

        if (user == null)
            return Result<AdminLoginResponse, AdminError>.Failure(AdminError.InvalidCredentials());

        if (await _userManager.IsLockedOutAsync(user))
        {
            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
            var remainingMinutes = lockoutEnd.HasValue
                ? Math.Max(1, (int)Math.Ceiling((lockoutEnd.Value - DateTimeOffset.UtcNow).TotalMinutes))
                : 5;
            return Result<AdminLoginResponse, AdminError>.Failure(AdminError.AccountLockedOut(remainingMinutes));
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
                return Result<AdminLoginResponse, AdminError>.Failure(AdminError.AccountLockedOut(remainingMinutes));
            }

            return Result<AdminLoginResponse, AdminError>.Failure(AdminError.InvalidCredentials());
        }

        var admin = await _dbContext.Admins
            .FirstOrDefaultAsync(a => (a.UserId == user.Id || a.Email.ToLower() == input.ToLower() || a.Phone == input) && a.IsActive, ct);

        if (admin == null)
            return Result<AdminLoginResponse, AdminError>.Failure(AdminError.InvalidCredentials());

        await _userManager.ResetAccessFailedCountAsync(user);

        if (string.IsNullOrEmpty(admin.UserId))
        {
            admin.UserId = user.Id;
        }

        admin.LastLoginAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);

        var (accessToken, refreshToken, expiresAt) = GenerateTokens(admin, user);

        var profile = new AdminProfileResponse(
            admin.Id,
            admin.FirstName,
            admin.LastName,
            admin.Email,
            admin.Phone,
            admin.Role.ToString(),
            admin.IsActive,
            admin.LastLoginAt
        );

        var response = new AdminLoginResponse(accessToken, refreshToken, expiresAt, profile);
        return Result<AdminLoginResponse, AdminError>.Success(response);
    }

    public async Task<AdminProfileResponse?> GetProfileAsync(Guid adminId, CancellationToken ct = default)
    {
        var admin = await _dbContext.Admins
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == adminId, ct);

        if (admin == null) return null;

        return new AdminProfileResponse(
            admin.Id,
            admin.FirstName,
            admin.LastName,
            admin.Email,
            admin.Phone,
            admin.Role.ToString(),
            admin.IsActive,
            admin.LastLoginAt
        );
    }

    private (string AccessToken, string RefreshToken, DateTime ExpiresAt) GenerateTokens(Admin admin, TaxiPayUser user)
    {
        var expiresAt = DateTime.UtcNow.AddHours(_jwtSettings.AccessTokenExpiryHours);

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id),
            new Claim("adminGuid", admin.Id.ToString()),
            new Claim(ClaimTypes.Name, $"{admin.FirstName} {admin.LastName}"),
            new Claim(ClaimTypes.Email, admin.Email),
            new Claim(ClaimTypes.MobilePhone, admin.Phone),
            new Claim(ClaimTypes.Role, "Admin"),
            new Claim("role", "admin"),
            new Claim("adminRole", admin.Role.ToString())
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

    // Driver Management
    public async Task<List<AdminDriverListItemDto>> GetDriversAsync(string? status = null, CancellationToken ct = default)
    {
        var query = _dbContext.Drivers
            .AsNoTracking()
            .Include(d => d.CurrentRoute)
            .Include(d => d.PreferredRoute)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<DriverStatus>(status, true, out var driverStatus))
        {
            query = query.Where(d => d.Status == driverStatus);
        }

        var drivers = await query
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync(ct);

        return drivers.Select(d => new AdminDriverListItemDto(
            d.Id,
            d.DriverId,
            d.FirstName,
            d.LastName,
            d.Phone,
            d.LicenseNumber,
            d.Status.ToString(),
            d.IsVerified,
            d.Rating,
            d.TotalTrips,
            d.TotalRevenue,
            d.CurrentRouteId,
            d.CurrentRoute?.RouteCode,
            d.CurrentRoute?.Name,
            d.PreferredRouteId,
            d.PreferredRoute?.RouteCode,
            d.PreferredRoute?.Name,
            d.RejectionReason,
            d.CreatedAt
        )).ToList();
    }

    public async Task<AdminDriverListItemDto?> GetDriverByIdAsync(Guid driverId, CancellationToken ct = default)
    {
        var d = await _dbContext.Drivers
            .AsNoTracking()
            .Include(d => d.CurrentRoute)
            .Include(d => d.PreferredRoute)
            .FirstOrDefaultAsync(d => d.Id == driverId, ct);

        if (d == null) return null;

        return new AdminDriverListItemDto(
            d.Id,
            d.DriverId,
            d.FirstName,
            d.LastName,
            d.Phone,
            d.LicenseNumber,
            d.Status.ToString(),
            d.IsVerified,
            d.Rating,
            d.TotalTrips,
            d.TotalRevenue,
            d.CurrentRouteId,
            d.CurrentRoute?.RouteCode,
            d.CurrentRoute?.Name,
            d.PreferredRouteId,
            d.PreferredRoute?.RouteCode,
            d.PreferredRoute?.Name,
            d.RejectionReason,
            d.CreatedAt
        );
    }

    public async Task<bool> UpdateDriverStatusAsync(Guid driverId, UpdateDriverStatusRequest request, CancellationToken ct = default)
    {
        var driver = await _dbContext.Drivers.FirstOrDefaultAsync(d => d.Id == driverId, ct);
        if (driver == null) return false;

        if (Enum.TryParse<DriverStatus>(request.Status, true, out var newStatus))
        {
            driver.Status = newStatus;
            if (newStatus == DriverStatus.Active)
            {
                driver.IsVerified = true;
                driver.VerifiedAt ??= DateTime.UtcNow;
                if (string.IsNullOrEmpty(driver.DriverId))
                {
                    driver.DriverId = $"DRV-{new Random().Next(1000, 9999)}";
                }
            }
            await _dbContext.SaveChangesAsync(ct);

            await _realtimeNotifier.NotifyDriverStatusUpdatedAsync(
                driver.Id,
                driver.DriverId,
                new DriverStatusUpdateDto(driver.Id, driver.Status.ToString(), driver.IsVerified, driver.DriverId, driver.RejectionReason),
                ct);

            return true;
        }

        return false;
    }

    public async Task<bool> ApproveDriverAsync(Guid driverId, ApproveDriverRequest request, CancellationToken ct = default)
    {
        using var activity = TaxiPayDiagnostics.ActivitySource.StartActivity("Admin.ApproveDriver");
        activity?.SetTag("driver.id", driverId.ToString());

        var driver = await _dbContext.Drivers.FirstOrDefaultAsync(d => d.Id == driverId, ct);
        if (driver == null) return false;

        var assignedRouteId = request.AssignedRouteId ?? driver.PreferredRouteId ?? driver.CurrentRouteId;
        if (assignedRouteId.HasValue)
        {
            var route = await _dbContext.Routes.FirstOrDefaultAsync(r => r.Id == assignedRouteId.Value && r.IsActive, ct);
            if (route != null)
            {
                driver.CurrentRouteId = route.Id;
            }
        }

        driver.Status = DriverStatus.Active;
        driver.IsVerified = true;
        driver.VerifiedAt ??= DateTime.UtcNow;
        driver.RejectionReason = null;
        if (string.IsNullOrEmpty(driver.DriverId))
        {
            driver.DriverId = $"DR{new Random().Next(1000, 9999)}";
        }

        await _dbContext.SaveChangesAsync(ct);

        await _realtimeNotifier.NotifyDriverStatusUpdatedAsync(
            driver.Id,
            driver.DriverId,
            new DriverStatusUpdateDto(driver.Id, driver.Status.ToString(), driver.IsVerified, driver.DriverId, null),
            ct);

        return true;
    }

    public async Task<bool> RejectDriverAsync(Guid driverId, RejectDriverRequest request, CancellationToken ct = default)
    {
        using var activity = TaxiPayDiagnostics.ActivitySource.StartActivity("Admin.RejectDriver");
        activity?.SetTag("driver.id", driverId.ToString());

        if (string.IsNullOrWhiteSpace(request.RejectionReason)) return false;

        var driver = await _dbContext.Drivers.FirstOrDefaultAsync(d => d.Id == driverId, ct);
        if (driver == null) return false;

        driver.Status = DriverStatus.Rejected;
        driver.RejectionReason = request.RejectionReason.Trim();
        driver.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(ct);

        await _realtimeNotifier.NotifyDriverStatusUpdatedAsync(
            driver.Id,
            driver.DriverId,
            new DriverStatusUpdateDto(driver.Id, driver.Status.ToString(), driver.IsVerified, driver.DriverId, driver.RejectionReason),
            ct);

        return true;
    }

    public async Task<bool> AssignDriverRouteAsync(Guid driverId, AssignDriverRouteRequest request, CancellationToken ct = default)
    {
        var driver = await _dbContext.Drivers.FirstOrDefaultAsync(d => d.Id == driverId, ct);
        if (driver == null) return false;

        var route = await _dbContext.Routes.FirstOrDefaultAsync(r => r.Id == request.RouteId && r.IsActive, ct);
        if (route == null) return false;

        driver.CurrentRouteId = route.Id;
        await _dbContext.SaveChangesAsync(ct);

        await _realtimeNotifier.NotifyRouteChangeReviewedAsync(
            driver.Id,
            driver.DriverId,
            new RouteChangeNotificationDto(driver.Id, route.Id, route.RouteCode, route.Name, true, "Assigned by admin"),
            ct);

        return true;
    }

    // Route Management
    public async Task<List<RouteDetailDto>> GetRoutesAsync(CancellationToken ct = default)
    {
        var routes = await _dbContext.Routes
            .AsNoTracking()
            .Include(r => r.Shifts)
            .OrderBy(r => r.RouteCode)
            .ToListAsync(ct);

        return routes.Select(r => new RouteDetailDto(
            r.Id,
            r.Name,
            r.RouteCode,
            r.Nodes.Select(n => new AdminNodeDto(n.Id, n.Name, n.Latitude, n.Longitude)).ToList(),
            r.IsActive,
            r.Shifts.Count(s => s.Status == ShiftStatus.Active),
            r.CreatedAt
        )).ToList();
    }

    public async Task<RouteDetailDto?> GetRouteByIdAsync(Guid routeId, CancellationToken ct = default)
    {
        var r = await _dbContext.Routes
            .AsNoTracking()
            .Include(r => r.Shifts)
            .FirstOrDefaultAsync(r => r.Id == routeId, ct);

        if (r == null) return null;

        return new RouteDetailDto(
            r.Id,
            r.Name,
            r.RouteCode,
            r.Nodes.Select(n => new AdminNodeDto(n.Id, n.Name, n.Latitude, n.Longitude)).ToList(),
            r.IsActive,
            r.Shifts.Count(s => s.Status == ShiftStatus.Active),
            r.CreatedAt
        );
    }

    public async Task<RouteDetailDto> CreateRouteAsync(CreateRouteRequest request, CancellationToken ct = default)
    {
        var route = new Domain.Entities.Route
        {
            Name = request.Name,
            RouteCode = request.RouteCode,
            Nodes = request.Nodes.Select(n => new Domain.Entities.Node
            {
                Id = n.Id,
                Name = n.Name,
                Latitude = n.Latitude,
                Longitude = n.Longitude
            }).ToList(),
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.Routes.Add(route);
        await _dbContext.SaveChangesAsync(ct);

        return new RouteDetailDto(
            route.Id,
            route.Name,
            route.RouteCode,
            route.Nodes.Select(n => new AdminNodeDto(n.Id, n.Name, n.Latitude, n.Longitude)).ToList(),
            route.IsActive,
            0,
            route.CreatedAt
        );
    }

    public async Task<RouteDetailDto?> UpdateRouteAsync(Guid routeId, UpdateRouteRequest request, CancellationToken ct = default)
    {
        var route = await _dbContext.Routes.FirstOrDefaultAsync(r => r.Id == routeId, ct);
        if (route == null) return null;

        route.Name = request.Name;
        route.RouteCode = request.RouteCode;
        route.Nodes = request.Nodes.Select(n => new Domain.Entities.Node
        {
            Id = n.Id,
            Name = n.Name,
            Latitude = n.Latitude,
            Longitude = n.Longitude
        }).ToList();
        route.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(ct);

        return new RouteDetailDto(
            route.Id,
            route.Name,
            route.RouteCode,
            route.Nodes.Select(n => new AdminNodeDto(n.Id, n.Name, n.Latitude, n.Longitude)).ToList(),
            route.IsActive,
            0,
            route.CreatedAt
        );
    }

    public async Task<bool> ToggleRouteStatusAsync(Guid routeId, CancellationToken ct = default)
    {
        var route = await _dbContext.Routes.FirstOrDefaultAsync(r => r.Id == routeId, ct);
        if (route == null) return false;

        route.IsActive = !route.IsActive;
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    public async Task<bool> DeleteRouteAsync(Guid routeId, CancellationToken ct = default)
    {
        var route = await _dbContext.Routes.FirstOrDefaultAsync(r => r.Id == routeId, ct);
        if (route == null) return false;

        _dbContext.Routes.Remove(route);
        await _dbContext.SaveChangesAsync(ct);
        return true;
    }

    // Fare Management
    public async Task<List<FareMatrixDto>> GetFareMatricesByRouteAsync(Guid routeId, CancellationToken ct = default)
    {
        var fares = await _dbContext.FareMatrices
            .AsNoTracking()
            .Include(f => f.Route)
            .Where(f => f.RouteId == routeId)
            .OrderBy(f => f.FromNode)
            .ThenBy(f => f.ToNode)
            .ToListAsync(ct);

        return fares.Select(f => new FareMatrixDto(
            f.Id,
            f.RouteId ?? Guid.Empty,
            f.Route?.RouteCode ?? string.Empty,
            f.FromNode,
            f.ToNode,
            f.Fare,
            f.IsActive
        )).ToList();
    }

    public async Task<FareMatrixDto> CreateFareMatrixAsync(CreateFareMatrixRequest request, CancellationToken ct = default)
    {
        var route = await _dbContext.Routes.FirstOrDefaultAsync(r => r.Id == request.RouteId, ct);
        var fareMatrix = new FareMatrix
        {
            RouteId = request.RouteId,
            FromNode = request.FromNode,
            ToNode = request.ToNode,
            Fare = request.Fare,
            IsActive = request.IsActive,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.FareMatrices.Add(fareMatrix);
        await _dbContext.SaveChangesAsync(ct);

        return new FareMatrixDto(
            fareMatrix.Id,
            fareMatrix.RouteId.Value,
            route?.RouteCode ?? string.Empty,
            fareMatrix.FromNode,
            fareMatrix.ToNode,
            fareMatrix.Fare,
            fareMatrix.IsActive
        );
    }

    public async Task<FareMatrixDto?> UpdateFareMatrixAsync(Guid id, UpdateFareMatrixRequest request, CancellationToken ct = default)
    {
        var fareMatrix = await _dbContext.FareMatrices
            .Include(f => f.Route)
            .FirstOrDefaultAsync(f => f.Id == id, ct);

        if (fareMatrix == null) return null;

        fareMatrix.Fare = request.Fare;
        fareMatrix.IsActive = request.IsActive;

        await _dbContext.SaveChangesAsync(ct);

        return new FareMatrixDto(
            fareMatrix.Id,
            fareMatrix.RouteId ?? Guid.Empty,
            fareMatrix.Route?.RouteCode ?? string.Empty,
            fareMatrix.FromNode,
            fareMatrix.ToNode,
            fareMatrix.Fare,
            fareMatrix.IsActive
        );
    }

    public async Task<List<FareMatrixDto>> GenerateSymmetricFaresAsync(GenerateSymmetricFareRequest request, CancellationToken ct = default)
    {
        var route = await _dbContext.Routes.FirstOrDefaultAsync(r => r.Id == request.RouteId, ct);
        if (route == null || route.Nodes == null || route.Nodes.Count < 2)
            return new List<FareMatrixDto>();

        // Remove existing fares for this route
        var existing = await _dbContext.FareMatrices.Where(f => f.RouteId == request.RouteId).ToListAsync(ct);
        _dbContext.FareMatrices.RemoveRange(existing);

        var newFares = new List<FareMatrix>();
        var nodes = route.Nodes;

        for (int i = 0; i < nodes.Count; i++)
        {
            for (int j = i + 1; j < nodes.Count; j++)
            {
                int hops = j - i;
                decimal fareAmount = Math.Max(5, hops * request.BaseFarePerHop);

                // Forward
                newFares.Add(new FareMatrix
                {
                    RouteId = route.Id,
                    FromNode = nodes[i].Id,
                    ToNode = nodes[j].Id,
                    Fare = fareAmount,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });

                // Reverse
                newFares.Add(new FareMatrix
                {
                    RouteId = route.Id,
                    FromNode = nodes[j].Id,
                    ToNode = nodes[i].Id,
                    Fare = fareAmount,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                });
            }
        }

        await _dbContext.FareMatrices.AddRangeAsync(newFares, ct);
        await _dbContext.SaveChangesAsync(ct);

        return newFares.Select(f => new FareMatrixDto(
            f.Id,
            f.RouteId.Value,
            route.RouteCode,
            f.FromNode,
            f.ToNode,
            f.Fare,
            f.IsActive
        )).ToList();
    }

    // Route Change Requests
    public async Task<List<AdminRouteChangeRequestDto>> GetRouteChangeRequestsAsync(string? status = null, CancellationToken ct = default)
    {
        var query = _dbContext.RouteChangeRequests
            .AsNoTracking()
            .Include(r => r.Driver)
            .Include(r => r.CurrentRoute)
            .Include(r => r.NewRoute)
            .Include(r => r.ReviewedByAdmin)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<RouteChangeStatus>(status, true, out var routeChangeStatus))
        {
            query = query.Where(r => r.Status == routeChangeStatus);
        }

        var requests = await query.OrderByDescending(r => r.RequestedAt).ToListAsync(ct);

        // Check if drivers have active trips
        var activeDriverIds = await _dbContext.Trips
            .Where(t => t.Status == TripStatus.Active)
            .Select(t => t.DriverId)
            .Distinct()
            .ToListAsync(ct);

        return requests.Select(r => new AdminRouteChangeRequestDto(
            r.Id,
            r.DriverId,
            $"{r.Driver?.FirstName} {r.Driver?.LastName}".Trim(),
            r.Driver?.DriverId,
            r.Driver?.Phone ?? string.Empty,
            r.CurrentRouteId,
            r.CurrentRoute?.RouteCode ?? string.Empty,
            r.CurrentRoute?.Name ?? string.Empty,
            r.NewRouteId,
            r.NewRoute?.RouteCode ?? string.Empty,
            r.NewRoute?.Name ?? string.Empty,
            r.Reason,
            r.Status.ToString(),
            activeDriverIds.Contains(r.DriverId),
            r.RequestedAt,
            r.ReviewedAt,
            r.RejectionReason,
            r.ReviewedByAdmin != null ? $"{r.ReviewedByAdmin.FirstName} {r.ReviewedByAdmin.LastName}".Trim() : null
        )).ToList();
    }

    public async Task<bool> ReviewRouteChangeRequestAsync(Guid requestId, Guid adminId, ReviewRouteChangeRequestDto request, CancellationToken ct = default)
    {
        var req = await _dbContext.RouteChangeRequests
            .Include(r => r.Driver)
            .Include(r => r.NewRoute)
            .FirstOrDefaultAsync(r => r.Id == requestId, ct);

        if (req == null || req.Status != RouteChangeStatus.Pending)
            return false;

        // Check active trip safety
        var hasActiveTrip = await _dbContext.Trips
            .AnyAsync(t => t.DriverId == req.DriverId && t.Status == TripStatus.Active, ct);

        if (request.Approved)
        {
            if (hasActiveTrip)
                return false; // Cannot switch route during active trip

            req.Status = RouteChangeStatus.Approved;
            if (req.Driver != null)
            {
                req.Driver.CurrentRouteId = req.NewRouteId;
                req.Driver.PreferredRouteId = req.NewRouteId;

                var activeShift = await _dbContext.DriverShifts
                    .FirstOrDefaultAsync(s => s.DriverId == req.DriverId && s.Status == ShiftStatus.Active, ct);
                if (activeShift != null)
                {
                    activeShift.RouteId = req.NewRouteId;
                }
            }
        }
        else
        {
            req.Status = RouteChangeStatus.Rejected;
            req.RejectionReason = request.Note;
        }

        req.ReviewedAt = DateTime.UtcNow;
        req.ReviewedByAdminId = adminId;

        await _dbContext.SaveChangesAsync(ct);

        await _realtimeNotifier.NotifyRouteChangeReviewedAsync(
            req.DriverId,
            req.Driver?.DriverId,
            new RouteChangeNotificationDto(req.DriverId, req.NewRouteId, req.NewRoute?.RouteCode, req.NewRoute?.Name, request.Approved, request.Note),
            ct);

        if (request.Approved)
        {
            await CreateAndBroadcastNotificationAsync(
                req.DriverId,
                "Driver",
                "Route Transfer Approved",
                $"Your route transfer request to corridor {req.NewRoute?.Name ?? "New Route"} was approved.",
                "success",
                "/driver/dashboard",
                ct);
        }
        else
        {
            await CreateAndBroadcastNotificationAsync(
                req.DriverId,
                "Driver",
                "Route Transfer Rejected",
                $"Your route transfer request was rejected. Reason: {request.Note ?? "Administrative regulation."}",
                "warning",
                "/driver/route-change",
                ct);
        }

        return true;
    }

    // Dispute Resolution
    public async Task<List<AdminDisputeDto>> GetDisputesAsync(string? status = null, CancellationToken ct = default)
    {
        var query = _dbContext.Disputes
            .AsNoTracking()
            .Include(d => d.Trip)
                .ThenInclude(t => t!.Route)
            .Include(d => d.Passenger)
            .Include(d => d.Driver)
            .Include(d => d.ResolvedByAdmin)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
        {
            var s = status.Trim();
            if (s.Equals("Open", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(d => d.Status == DisputeStatus.Open || d.Status == DisputeStatus.Reviewing);
            }
            else if (s.Equals("Closed", StringComparison.OrdinalIgnoreCase) || s.Equals("Resolved", StringComparison.OrdinalIgnoreCase))
            {
                query = query.Where(d => d.Status == DisputeStatus.Closed || d.Status == DisputeStatus.Resolved || d.Status == DisputeStatus.Rejected);
            }
            else if (Enum.TryParse<DisputeStatus>(s, true, out var disputeStatus))
            {
                query = query.Where(d => d.Status == disputeStatus);
            }
        }

        var disputes = await query.OrderByDescending(d => d.CreatedAt).ToListAsync(ct);

        return disputes.Select(MapToDisputeDto).ToList();
    }

    public async Task<bool> OpenDisputeAsync(Guid disputeId, Guid adminId, string? notes, CancellationToken ct = default)
    {
        var dispute = await _dbContext.Disputes
            .Include(d => d.Passenger)
            .Include(d => d.Driver)
            .Include(d => d.Trip)
                .ThenInclude(t => t!.Route)
            .FirstOrDefaultAsync(d => d.Id == disputeId, ct);

        if (dispute == null) return false;

        dispute.Status = DisputeStatus.Open;
        if (!string.IsNullOrWhiteSpace(notes))
        {
            dispute.ResolutionNotes = notes;
        }
        dispute.ResolvedAt = null;
        dispute.ResolvedByAdminId = null;

        await _dbContext.SaveChangesAsync(ct);

        var disputeDto = MapToDisputeDto(dispute);
        await _realtimeNotifier.NotifyDisputeUpdatedAsync(disputeDto, ct);

        await CreateAndBroadcastNotificationAsync(
            dispute.PassengerId,
            "Passenger",
            "Dispute Case Reopened",
            $"Dispute for trip {disputeDto.TripNumber} has been reopened for administrative arbitration.",
            "info",
            "/passenger/dashboard",
            ct);

        return true;
    }

    public async Task<bool> CloseDisputeAsync(Guid disputeId, Guid adminId, string? resolutionNotes, bool issueRefund, CancellationToken ct = default)
    {
        var dispute = await _dbContext.Disputes
            .Include(d => d.Passenger)
            .Include(d => d.Driver)
            .Include(d => d.Trip)
                .ThenInclude(t => t!.Route)
            .FirstOrDefaultAsync(d => d.Id == disputeId, ct);

        if (dispute == null) return false;

        dispute.Status = DisputeStatus.Closed;
        dispute.ResolutionNotes = resolutionNotes ?? "Dispute closed by administrator.";
        dispute.ResolvedAt = DateTime.UtcNow;
        dispute.ResolvedByAdminId = adminId;

        if (issueRefund && dispute.Passenger != null && dispute.Trip != null)
        {
            var refundAmt = dispute.Trip.TripRevenue > 0 ? dispute.Trip.TripRevenue : 25;
            dispute.Passenger.WalletBalance += refundAmt;
        }

        await _dbContext.SaveChangesAsync(ct);

        var disputeDto = MapToDisputeDto(dispute);
        await _realtimeNotifier.NotifyDisputeUpdatedAsync(disputeDto, ct);

        var refundMsg = issueRefund ? " An automatic refund was credited to your wallet." : "";
        await CreateAndBroadcastNotificationAsync(
            dispute.PassengerId,
            "Passenger",
            "Dispute Case Closed",
            $"Dispute for trip {disputeDto.TripNumber} is now Closed. Resolution: {dispute.ResolutionNotes}.{refundMsg}",
            "success",
            "/passenger/dashboard",
            ct);

        return true;
    }

    public async Task<bool> ResolveDisputeAsync(Guid disputeId, Guid adminId, ReviewDisputeRequestDto request, CancellationToken ct = default)
    {
        if (request.Status.Equals("Open", StringComparison.OrdinalIgnoreCase))
        {
            return await OpenDisputeAsync(disputeId, adminId, request.ResolutionNotes, ct);
        }

        return await CloseDisputeAsync(disputeId, adminId, request.ResolutionNotes, request.IssueRefund, ct);
    }

    private static AdminDisputeDto MapToDisputeDto(Dispute d)
    {
        var displayStatus = d.Status switch
        {
            DisputeStatus.Closed or DisputeStatus.Resolved or DisputeStatus.Rejected => "Closed",
            _ => "Open"
        };

        return new AdminDisputeDto(
            d.Id,
            d.TripId,
            d.Trip != null ? $"TRIP-{d.Trip.Id.ToString()[..6].ToUpper()}" : "N/A",
            d.PassengerId,
            d.Passenger != null ? $"{d.Passenger.FirstName} {d.Passenger.LastName}".Trim() : "N/A",
            d.Passenger?.Phone ?? string.Empty,
            d.DriverId,
            d.Driver != null ? $"{d.Driver.FirstName} {d.Driver.LastName}".Trim() : "N/A",
            d.Driver?.Phone ?? string.Empty,
            d.Trip?.Route?.RouteCode ?? "N/A",
            d.Trip?.Route?.Name ?? "N/A",
            "A",
            "F",
            d.Trip?.TripRevenue ?? 25,
            d.Type.ToString(),
            d.Description,
            displayStatus,
            d.ResolutionNotes,
            d.ResolvedAt,
            d.ResolvedByAdmin != null ? $"{d.ResolvedByAdmin.FirstName} {d.ResolvedByAdmin.LastName}".Trim() : null,
            d.CreatedAt
        );
    }

    private async Task CreateAndBroadcastNotificationAsync(
        Guid? userId,
        string? role,
        string title,
        string message,
        string type = "info",
        string? targetUrl = null,
        CancellationToken ct = default)
    {
        try
        {
            var notif = new AppNotification
            {
                UserId = userId,
                Role = role,
                Title = title,
                Message = message,
                Type = type,
                TargetUrl = targetUrl,
                IsRead = false,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.AppNotifications.Add(notif);
            await _dbContext.SaveChangesAsync(ct);

            var dto = new AppNotificationDto(
                notif.Id,
                notif.UserId,
                notif.Role,
                notif.Title,
                notif.Message,
                notif.Type,
                notif.TargetUrl,
                notif.IsRead,
                notif.CreatedAt
            );

            await _realtimeNotifier.NotifyNotificationCreatedAsync(dto, role, userId, ct);
        }
        catch
        {
            // Non-blocking notification dispatch
        }
    }

    // System Monitoring & Analytics
    public async Task<AdminAnalyticsOverviewDto> GetAnalyticsOverviewAsync(CancellationToken ct = default)
    {
        var totalRevenue = await _dbContext.Trips.SumAsync(t => t.TripRevenue, ct);
        var today = DateTime.UtcNow.Date;
        var todayRevenue = await _dbContext.Trips
            .Where(t => t.CreatedAt >= today)
            .SumAsync(t => t.TripRevenue, ct);

        var totalTrips = await _dbContext.Trips.CountAsync(ct);
        var activeTrips = await _dbContext.Trips.CountAsync(t => t.Status == TripStatus.Active, ct);
        var totalDrivers = await _dbContext.Drivers.CountAsync(ct);
        var activeDriversOnShift = await _dbContext.DriverShifts.CountAsync(s => s.Status == ShiftStatus.Active, ct);
        var pendingDriverApprovals = await _dbContext.Drivers.CountAsync(d => d.Status == DriverStatus.Pending, ct);
        var totalPassengers = await _dbContext.Passengers.CountAsync(ct);
        var openDisputes = await _dbContext.Disputes.CountAsync(d => d.Status == DisputeStatus.Open || d.Status == DisputeStatus.Reviewing, ct);
        var pendingRouteRequests = await _dbContext.RouteChangeRequests.CountAsync(r => r.Status == RouteChangeStatus.Pending, ct);

        // Daily history for past 7 days
        var dailyStats = new List<DailyRevenueStatDto>();
        for (int i = 6; i >= 0; i--)
        {
            var date = DateTime.UtcNow.Date.AddDays(-i);
            var nextDate = date.AddDays(1);
            var dayRev = await _dbContext.Trips
                .Where(t => t.CreatedAt >= date && t.CreatedAt < nextDate)
                .SumAsync(t => t.TripRevenue, ct);
            var dayTrips = await _dbContext.Trips
                .CountAsync(t => t.CreatedAt >= date && t.CreatedAt < nextDate, ct);

            dailyStats.Add(new DailyRevenueStatDto(date.ToString("MMM dd"), dayRev, dayTrips));
        }

        var recentActivities = new List<RecentActivityDto>();

        // Recent Trips
        var latestTrips = await _dbContext.Trips
            .Include(t => t.Route)
            .OrderByDescending(t => t.CreatedAt)
            .Take(2)
            .ToListAsync(ct);
        foreach (var t in latestTrips)
        {
            recentActivities.Add(new RecentActivityDto(
                $"TRIP-{t.Id.ToString()[..6]}",
                "Trip",
                t.Status == TripStatus.Active ? "Live Trip in Progress" : "Completed Minibus Run",
                $"{t.Route?.Name ?? "Corridor"}: {t.PassengersCount} passengers on board",
                t.CreatedAt,
                t.Status.ToString()
            ));
        }

        // Recent Driver Applications
        var latestDrivers = await _dbContext.Drivers
            .OrderByDescending(d => d.CreatedAt)
            .Take(2)
            .ToListAsync(ct);
        foreach (var d in latestDrivers)
        {
            recentActivities.Add(new RecentActivityDto(
                $"DRV-{d.Id.ToString()[..6]}",
                "DriverRegistration",
                d.Status == DriverStatus.Pending ? "New Driver Application" : "Driver Accredited",
                $"{d.FirstName} {d.LastName} ({d.LicenseNumber})",
                d.CreatedAt,
                d.Status.ToString()
            ));
        }

        // Recent Route Requests
        var latestRouteReqs = await _dbContext.RouteChangeRequests
            .Include(r => r.Driver)
            .Include(r => r.NewRoute)
            .OrderByDescending(r => r.CreatedAt)
            .Take(2)
            .ToListAsync(ct);
        foreach (var r in latestRouteReqs)
        {
            recentActivities.Add(new RecentActivityDto(
                $"REQ-{r.Id.ToString()[..6]}",
                "RouteChange",
                "Route Transfer Request",
                $"{r.Driver?.FirstName ?? "Driver"} requested {r.NewRoute?.Name ?? "Route"}",
                r.CreatedAt,
                r.Status.ToString()
            ));
        }

        // Corridor Metrics from Database
        var allRoutes = await _dbContext.Routes
            .AsNoTracking()
            .Include(r => r.Trips)
            .Include(r => r.Shifts)
            .ToListAsync(ct);

        var corridorStats = new List<CorridorStatDto>();
        foreach (var route in allRoutes)
        {
            var routeRevenue = route.Trips.Sum(t => t.TripRevenue);
            var routeTripsCount = route.Trips.Count;
            var activeShiftCount = route.Shifts.Count(s => s.Status == ShiftStatus.Active);
            var share = totalRevenue > 0 ? Math.Round((double)(routeRevenue / totalRevenue) * 100, 1) : 0;
            var peakNode = route.Nodes.FirstOrDefault()?.Name ?? "Terminal";

            corridorStats.Add(new CorridorStatDto(
                route.RouteCode,
                route.Name,
                routeRevenue,
                share,
                activeShiftCount,
                routeTripsCount,
                peakNode
            ));
        }

        return new AdminAnalyticsOverviewDto(
            totalRevenue,
            todayRevenue,
            totalTrips,
            activeTrips,
            totalDrivers,
            activeDriversOnShift,
            pendingDriverApprovals,
            totalPassengers,
            openDisputes,
            pendingRouteRequests,
            dailyStats,
            recentActivities,
            corridorStats
        );
    }
}
