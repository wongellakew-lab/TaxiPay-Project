using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Application.DTOs.Notifications;
using TaxiPayApi.Application.DTOs.Passengers;
using TaxiPayApi.Application.DTOs.Realtime;

namespace TaxiPayApi.Application.Interfaces;

public interface ITaxiPayRealtimeNotifier
{
    Task NotifyPassengerAddedToManifestAsync(string driverIdentifier, Guid driverGuid, ActivePassengerDto passenger, CancellationToken cancellationToken = default);
    Task NotifyAlightingRequestedAsync(string driverIdentifier, Guid driverGuid, Guid passengerId, ActivePassengerDto passenger, CancellationToken cancellationToken = default);
    Task NotifyAlightingConfirmedAsync(string driverIdentifier, Guid driverGuid, Guid passengerId, ActivePassengerDto passenger, CancellationToken cancellationToken = default);
    Task NotifyTripStatusUpdatedAsync(Guid tripId, List<Guid> passengerIds, string? driverIdentifier, TripStatusUpdateDto status, CancellationToken cancellationToken = default);
    Task NotifyDriverStatusUpdatedAsync(Guid driverId, string? driverCode, DriverStatusUpdateDto status, CancellationToken cancellationToken = default);
    Task NotifyRouteChangeReviewedAsync(Guid driverId, string? driverCode, RouteChangeNotificationDto update, CancellationToken cancellationToken = default);
    Task NotifyDriverRouteChangeSubmittedAsync(AdminRouteChangeRequestDto request, CancellationToken cancellationToken = default);
    Task NotifyPassengerRouteRequestSubmittedAsync(PassengerRouteRequestDto request, CancellationToken cancellationToken = default);
    Task NotifyPassengerRouteRequestReviewedAsync(Guid passengerId, PassengerRouteRequestNotificationDto notification, CancellationToken cancellationToken = default);
    Task NotifyDisputeUpdatedAsync(AdminDisputeDto dispute, CancellationToken cancellationToken = default);
    Task NotifyNotificationCreatedAsync(AppNotificationDto notification, string? role = null, Guid? userId = null, CancellationToken cancellationToken = default);
}
