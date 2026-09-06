using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Application.DTOs.Notifications;
using TaxiPayApi.Application.DTOs.Passengers;
using TaxiPayApi.Application.DTOs.Realtime;
using TaxiPayApi.Application.Interfaces;
using TaxiPayApi.Infrastructure.Hubs;

namespace TaxiPayApi.Infrastructure.Services;

public class TaxiPayRealtimeNotifier : ITaxiPayRealtimeNotifier
{
    private readonly IHubContext<TaxiPayHub, ITaxiPayHubClient> _hubContext;
    private readonly ILogger<TaxiPayRealtimeNotifier> _logger;

    public TaxiPayRealtimeNotifier(
        IHubContext<TaxiPayHub, ITaxiPayHubClient> hubContext,
        ILogger<TaxiPayRealtimeNotifier> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task NotifyPassengerAddedToManifestAsync(
        string driverIdentifier,
        Guid driverGuid,
        ActivePassengerDto passenger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var groups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(driverIdentifier))
            {
                groups.Add($"driver:{driverIdentifier.Trim().ToLowerInvariant()}");
            }
            if (driverGuid != Guid.Empty)
            {
                groups.Add($"driver:{driverGuid.ToString().ToLowerInvariant()}");
            }

            foreach (var groupName in groups)
            {
                _logger.LogInformation("Broadcasting ReceivePassengerAddedToManifest for JourneyId {JourneyId} to SignalR group {Group}",
                    passenger.JourneyId, groupName);
                await _hubContext.Clients.Group(groupName).ReceivePassengerAddedToManifest(passenger);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send SignalR ReceivePassengerAddedToManifest event for driver {DriverIdentifier}", driverIdentifier);
        }
    }

    public async Task NotifyAlightingRequestedAsync(
        string driverIdentifier,
        Guid driverGuid,
        Guid passengerId,
        ActivePassengerDto passenger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var groups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(driverIdentifier))
            {
                groups.Add($"driver:{driverIdentifier.Trim().ToLowerInvariant()}");
            }
            if (driverGuid != Guid.Empty)
            {
                groups.Add($"driver:{driverGuid.ToString().ToLowerInvariant()}");
            }
            if (passengerId != Guid.Empty)
            {
                groups.Add($"passenger:{passengerId.ToString().ToLowerInvariant()}");
            }

            foreach (var groupName in groups)
            {
                await _hubContext.Clients.Group(groupName).ReceiveAlightingRequested(passenger);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send SignalR ReceiveAlightingRequested for journey {JourneyId}", passenger.JourneyId);
        }
    }

    public async Task NotifyAlightingConfirmedAsync(
        string driverIdentifier,
        Guid driverGuid,
        Guid passengerId,
        ActivePassengerDto passenger,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var groups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrWhiteSpace(driverIdentifier))
            {
                groups.Add($"driver:{driverIdentifier.Trim().ToLowerInvariant()}");
            }
            if (driverGuid != Guid.Empty)
            {
                groups.Add($"driver:{driverGuid.ToString().ToLowerInvariant()}");
            }
            if (passengerId != Guid.Empty)
            {
                groups.Add($"passenger:{passengerId.ToString().ToLowerInvariant()}");
            }

            foreach (var groupName in groups)
            {
                await _hubContext.Clients.Group(groupName).ReceiveAlightingConfirmed(passenger);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send SignalR ReceiveAlightingConfirmed for journey {JourneyId}", passenger.JourneyId);
        }
    }

    public async Task NotifyTripStatusUpdatedAsync(
        Guid tripId,
        List<Guid> passengerIds,
        string? driverIdentifier,
        TripStatusUpdateDto status,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var groups = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                $"trip:{tripId.ToString().ToLowerInvariant()}",
                "passengers",
                "drivers",
                "admins"
            };

            if (!string.IsNullOrWhiteSpace(driverIdentifier))
            {
                groups.Add($"driver:{driverIdentifier.Trim().ToLowerInvariant()}");
            }

            foreach (var pid in passengerIds)
            {
                groups.Add($"passenger:{pid.ToString().ToLowerInvariant()}");
            }

            foreach (var groupName in groups)
            {
                await _hubContext.Clients.Group(groupName).ReceiveTripStatusUpdated(status);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send SignalR ReceiveTripStatusUpdated for trip {TripId}", tripId);
        }
    }

    public async Task NotifyDriverStatusUpdatedAsync(
        Guid driverId,
        string? driverCode,
        DriverStatusUpdateDto status,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var groups = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                $"driver:{driverId.ToString().ToLowerInvariant()}",
                "admins"
            };
            if (!string.IsNullOrWhiteSpace(driverCode))
            {
                groups.Add($"driver:{driverCode.Trim().ToLowerInvariant()}");
            }

            foreach (var groupName in groups)
            {
                await _hubContext.Clients.Group(groupName).ReceiveDriverStatusUpdated(status);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send SignalR ReceiveDriverStatusUpdated for driver {DriverId}", driverId);
        }
    }

    public async Task NotifyRouteChangeReviewedAsync(
        Guid driverId,
        string? driverCode,
        RouteChangeNotificationDto update,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var groups = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                $"driver:{driverId.ToString().ToLowerInvariant()}",
                $"user:{driverId.ToString().ToLowerInvariant()}"
            };
            if (!string.IsNullOrWhiteSpace(driverCode))
            {
                groups.Add($"driver:{driverCode.Trim().ToLowerInvariant()}");
                groups.Add($"user:{driverCode.Trim().ToLowerInvariant()}");
            }

            foreach (var groupName in groups)
            {
                await _hubContext.Clients.Group(groupName).ReceiveRouteChangeReviewed(update);
            }

            // Also inform admins group in real time
            await _hubContext.Clients.Group("admins").ReceiveRouteChangeReviewed(update);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send SignalR ReceiveRouteChangeReviewed for driver {DriverId}", driverId);
        }
    }

    public async Task NotifyDriverRouteChangeSubmittedAsync(
        AdminRouteChangeRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.Group("admins").ReceiveDriverRouteChangeSubmitted(request);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send SignalR ReceiveDriverRouteChangeSubmitted for request {RequestId}", request.Id);
        }
    }

    public async Task NotifyPassengerRouteRequestSubmittedAsync(
        PassengerRouteRequestDto request,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.Group("admins").ReceivePassengerRouteRequestSubmitted(request);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send SignalR ReceivePassengerRouteRequestSubmitted for request {RequestId}", request.Id);
        }
    }

    public async Task NotifyPassengerRouteRequestReviewedAsync(
        Guid passengerId,
        PassengerRouteRequestNotificationDto notification,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await _hubContext.Clients.Group($"passenger:{passengerId.ToString().ToLowerInvariant()}")
                .ReceivePassengerRouteRequestReviewed(notification);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send SignalR ReceivePassengerRouteRequestReviewed for passenger {PassengerId}", passengerId);
        }
    }

    public async Task NotifyDisputeUpdatedAsync(
        AdminDisputeDto dispute,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Inform admins
            await _hubContext.Clients.Group("admins").ReceiveDisputeUpdated(dispute);

            // Inform affected passenger
            if (dispute.PassengerId != Guid.Empty)
            {
                await _hubContext.Clients.Group($"passenger:{dispute.PassengerId.ToString().ToLowerInvariant()}")
                    .ReceiveDisputeUpdated(dispute);
            }

            // Inform affected driver
            if (dispute.DriverId != Guid.Empty)
            {
                await _hubContext.Clients.Group($"driver:{dispute.DriverId.ToString().ToLowerInvariant()}")
                    .ReceiveDisputeUpdated(dispute);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send SignalR ReceiveDisputeUpdated for dispute {DisputeId}", dispute.Id);
        }
    }

    public async Task NotifyNotificationCreatedAsync(
        AppNotificationDto notification,
        string? role = null,
        Guid? userId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var groups = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            if (userId.HasValue && userId.Value != Guid.Empty)
            {
                var uid = userId.Value.ToString().ToLowerInvariant();
                groups.Add($"user:{uid}");
                groups.Add($"passenger:{uid}");
                groups.Add($"driver:{uid}");
            }
            else if (!string.IsNullOrWhiteSpace(role))
            {
                var r = role.Trim().ToLowerInvariant();
                if (r == "admin" || r == "superadmin")
                {
                    groups.Add("admins");
                }
                else if (r == "driver")
                {
                    groups.Add("drivers");
                }
                else if (r == "passenger")
                {
                    groups.Add("passengers");
                }
            }

            if (groups.Count > 0)
            {
                foreach (var groupName in groups)
                {
                    await _hubContext.Clients.Group(groupName).ReceiveNotificationCreated(notification);
                }
            }
            else
            {
                // Public system broadcast
                await _hubContext.Clients.All.ReceiveNotificationCreated(notification);
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send SignalR ReceiveNotificationCreated for notification {NotificationId}", notification.Id);
        }
    }
}
