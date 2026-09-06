namespace TaxiPayApi.Application.DTOs.Realtime;

public record TripStatusUpdateDto(
    Guid TripId,
    string Status,
    string? Direction,
    DateTime? TripStart,
    DateTime? TripEnd
);

public record DriverStatusUpdateDto(
    Guid DriverId,
    string Status,
    bool IsVerified,
    string? DriverCode,
    string? RejectionReason
);

public record RouteChangeNotificationDto(
    Guid DriverId,
    Guid? NewRouteId,
    string? NewRouteCode,
    string? NewRouteName,
    bool Approved,
    string? Note
);

public record PassengerRouteRequestNotificationDto(
    Guid RequestId,
    Guid PassengerId,
    string Status,
    string? AdminNote
);
