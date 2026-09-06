using System.Collections.Concurrent;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Application.DTOs.Notifications;
using TaxiPayApi.Application.DTOs.Passengers;
using TaxiPayApi.Application.DTOs.Realtime;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Tests.Fixtures;

public class TestRealtimeNotifier : ITaxiPayRealtimeNotifier
{
    public ConcurrentBag<ActivePassengerDto> ManifestAdditions { get; } = new();
    public ConcurrentBag<ActivePassengerDto> AlightingRequests { get; } = new();
    public ConcurrentBag<ActivePassengerDto> AlightingConfirmations { get; } = new();
    public ConcurrentBag<TripStatusUpdateDto> TripStatusUpdates { get; } = new();
    public ConcurrentBag<DriverStatusUpdateDto> DriverStatusUpdates { get; } = new();

    public Task NotifyPassengerAddedToManifestAsync(string driverIdentifier, Guid driverGuid, ActivePassengerDto passenger, CancellationToken cancellationToken = default)
    {
        ManifestAdditions.Add(passenger);
        return Task.CompletedTask;
    }

    public Task NotifyAlightingRequestedAsync(string driverIdentifier, Guid driverGuid, Guid passengerId, ActivePassengerDto passenger, CancellationToken cancellationToken = default)
    {
        AlightingRequests.Add(passenger);
        return Task.CompletedTask;
    }

    public Task NotifyAlightingConfirmedAsync(string driverIdentifier, Guid driverGuid, Guid passengerId, ActivePassengerDto passenger, CancellationToken cancellationToken = default)
    {
        AlightingConfirmations.Add(passenger);
        return Task.CompletedTask;
    }

    public Task NotifyTripStatusUpdatedAsync(Guid tripId, List<Guid> passengerIds, string? driverIdentifier, TripStatusUpdateDto status, CancellationToken cancellationToken = default)
    {
        TripStatusUpdates.Add(status);
        return Task.CompletedTask;
    }

    public Task NotifyDriverStatusUpdatedAsync(Guid driverId, string? driverCode, DriverStatusUpdateDto status, CancellationToken cancellationToken = default)
    {
        DriverStatusUpdates.Add(status);
        return Task.CompletedTask;
    }

    public Task NotifyRouteChangeReviewedAsync(Guid driverId, string? driverCode, RouteChangeNotificationDto update, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyDriverRouteChangeSubmittedAsync(AdminRouteChangeRequestDto request, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyPassengerRouteRequestSubmittedAsync(PassengerRouteRequestDto request, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyPassengerRouteRequestReviewedAsync(Guid passengerId, PassengerRouteRequestNotificationDto notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyDisputeUpdatedAsync(AdminDisputeDto dispute, CancellationToken cancellationToken = default) => Task.CompletedTask;
    public Task NotifyNotificationCreatedAsync(AppNotificationDto notification, string? role = null, Guid? userId = null, CancellationToken cancellationToken = default) => Task.CompletedTask;
}
