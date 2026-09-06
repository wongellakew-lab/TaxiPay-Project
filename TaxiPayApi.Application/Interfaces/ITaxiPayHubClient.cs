using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Application.DTOs.Notifications;
using TaxiPayApi.Application.DTOs.Passengers;
using TaxiPayApi.Application.DTOs.Realtime;

namespace TaxiPayApi.Application.Interfaces;

public interface ITaxiPayHubClient
{
    // Driver manifest updates
    Task ReceivePassengerAddedToManifest(ActivePassengerDto passenger);
    Task ReceiveAlightingRequested(ActivePassengerDto passenger);
    Task ReceiveAlightingConfirmed(ActivePassengerDto passenger);

    // Trip updates
    Task ReceiveTripStatusUpdated(TripStatusUpdateDto status);

    // Driver status / route updates
    Task ReceiveDriverStatusUpdated(DriverStatusUpdateDto status);
    Task ReceiveDriverRouteChangeSubmitted(AdminRouteChangeRequestDto request);
    Task ReceiveRouteChangeReviewed(RouteChangeNotificationDto update);

    // Passenger route requests
    Task ReceivePassengerRouteRequestSubmitted(PassengerRouteRequestDto request);
    Task ReceivePassengerRouteRequestReviewed(PassengerRouteRequestNotificationDto request);

    // Dispute updates
    Task ReceiveDisputeUpdated(AdminDisputeDto dispute);

    // Persistent and Realtime System Notifications
    Task ReceiveNotificationCreated(AppNotificationDto notification);
}
