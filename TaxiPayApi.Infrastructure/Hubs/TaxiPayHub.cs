using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Infrastructure.Hubs;

[Authorize]
public class TaxiPayHub : Hub<ITaxiPayHubClient>
{
    private readonly ILogger<TaxiPayHub> _logger;

    public TaxiPayHub(ILogger<TaxiPayHub> logger)
    {
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var connectionId = Context.ConnectionId;
        var userId = Context.UserIdentifier ?? Context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = Context.User?.FindFirstValue(ClaimTypes.Role) ?? Context.User?.FindFirstValue("role");

        _logger.LogInformation("Authenticated SignalR Client connected: {ConnectionId}, UserId: {UserId}, Role: {Role}",
            connectionId, userId ?? "Unknown", role ?? "None");

        if (!string.IsNullOrEmpty(userId))
        {
            var cleanUserId = userId.ToLowerInvariant();
            await Groups.AddToGroupAsync(connectionId, $"user:{cleanUserId}");
        }

        var passengerIdClaim = Context.User?.FindFirstValue("passengerId");
        if (!string.IsNullOrEmpty(passengerIdClaim))
        {
            var cleanPid = passengerIdClaim.ToLowerInvariant();
            await Groups.AddToGroupAsync(connectionId, $"passenger:{cleanPid}");
            await Groups.AddToGroupAsync(connectionId, $"user:{cleanPid}");
        }

        var driverGuidClaim = Context.User?.FindFirstValue("driverGuid");
        if (!string.IsNullOrEmpty(driverGuidClaim))
        {
            var cleanDgid = driverGuidClaim.ToLowerInvariant();
            await Groups.AddToGroupAsync(connectionId, $"driver:{cleanDgid}");
            await Groups.AddToGroupAsync(connectionId, $"user:{cleanDgid}");
        }

        var driverIdClaim = Context.User?.FindFirstValue("driverId");
        if (!string.IsNullOrEmpty(driverIdClaim))
        {
            var cleanDid = driverIdClaim.ToLowerInvariant();
            await Groups.AddToGroupAsync(connectionId, $"driver:{cleanDid}");
            await Groups.AddToGroupAsync(connectionId, $"user:{cleanDid}");
        }

        var adminGuidClaim = Context.User?.FindFirstValue("adminGuid");
        if (!string.IsNullOrEmpty(adminGuidClaim))
        {
            var cleanAgid = adminGuidClaim.ToLowerInvariant();
            await Groups.AddToGroupAsync(connectionId, $"user:{cleanAgid}");
            await Groups.AddToGroupAsync(connectionId, "admins");
        }

        if (string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase))
        {
            await Groups.AddToGroupAsync(connectionId, "admins");
        }
        else if (string.Equals(role, "Driver", StringComparison.OrdinalIgnoreCase))
        {
            await Groups.AddToGroupAsync(connectionId, "drivers");
        }
        else if (string.Equals(role, "Passenger", StringComparison.OrdinalIgnoreCase))
        {
            await Groups.AddToGroupAsync(connectionId, "passengers");
        }

        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        _logger.LogInformation("SignalR Client disconnected: {ConnectionId}", Context.ConnectionId);
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinDriverGroup(string driverId)
    {
        if (string.IsNullOrWhiteSpace(driverId)) return;

        var cleanId = driverId.Trim().ToLowerInvariant();
        var callerDriverGuid = Context.User?.FindFirstValue("driverGuid")?.ToLowerInvariant();
        var callerDriverCode = Context.User?.FindFirstValue("driverId")?.ToLowerInvariant();
        var isAdmin = Context.User?.IsInRole("Admin") == true || Context.User?.IsInRole("SuperAdmin") == true;

        if (!isAdmin && cleanId != callerDriverGuid && cleanId != callerDriverCode)
        {
            _logger.LogWarning("Unauthorized JoinDriverGroup attempt from {ConnectionId} for target {DriverId}",
                Context.ConnectionId, cleanId);
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"driver:{cleanId}");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{cleanId}");
        _logger.LogDebug("Client {ConnectionId} authorized and joined driver group: driver:{DriverId}", Context.ConnectionId, cleanId);
    }

    public async Task LeaveDriverGroup(string driverId)
    {
        if (!string.IsNullOrWhiteSpace(driverId))
        {
            var cleanId = driverId.Trim().ToLowerInvariant();
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"driver:{cleanId}");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{cleanId}");
        }
    }

    public async Task JoinPassengerGroup(string passengerId)
    {
        if (string.IsNullOrWhiteSpace(passengerId)) return;

        var cleanId = passengerId.Trim().ToLowerInvariant();
        var callerPassengerId = Context.User?.FindFirstValue("passengerId")?.ToLowerInvariant();
        var callerUserId = Context.User?.FindFirstValue(ClaimTypes.NameIdentifier)?.ToLowerInvariant();
        var isAdmin = Context.User?.IsInRole("Admin") == true || Context.User?.IsInRole("SuperAdmin") == true;

        if (!isAdmin && cleanId != callerPassengerId && cleanId != callerUserId)
        {
            _logger.LogWarning("Unauthorized JoinPassengerGroup attempt from {ConnectionId} for target {PassengerId}",
                Context.ConnectionId, cleanId);
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, $"passenger:{cleanId}");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user:{cleanId}");
        _logger.LogDebug("Client {ConnectionId} authorized and joined passenger group: passenger:{PassengerId}", Context.ConnectionId, cleanId);
    }

    public async Task LeavePassengerGroup(string passengerId)
    {
        if (!string.IsNullOrWhiteSpace(passengerId))
        {
            var cleanId = passengerId.Trim().ToLowerInvariant();
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"passenger:{cleanId}");
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user:{cleanId}");
        }
    }

    public async Task JoinTripGroup(string tripId)
    {
        if (!string.IsNullOrWhiteSpace(tripId))
        {
            var cleanId = tripId.Trim().ToLowerInvariant();
            await Groups.AddToGroupAsync(Context.ConnectionId, $"trip:{cleanId}");
        }
    }

    public async Task LeaveTripGroup(string tripId)
    {
        if (!string.IsNullOrWhiteSpace(tripId))
        {
            var cleanId = tripId.Trim().ToLowerInvariant();
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"trip:{cleanId}");
        }
    }

    public async Task JoinAdminGroup()
    {
        if (Context.User?.IsInRole("Admin") != true && Context.User?.IsInRole("SuperAdmin") != true)
        {
            _logger.LogWarning("Unauthorized JoinAdminGroup attempt rejected from {ConnectionId}", Context.ConnectionId);
            return;
        }

        await Groups.AddToGroupAsync(Context.ConnectionId, "admins");
    }
}
