namespace TaxiPayApi.Application.Common;

public sealed record DriverError(string Code, string Message)
{
    // Registration Errors
    public static DriverError PhoneAlreadyExists() =>
        new("phone_already_exists", "This phone number is already registered.");

    public static DriverError LicenseAlreadyExists() =>
        new("license_already_exists", "This license number is already registered.");

    public static DriverError RouteNotFound(Guid routeId) =>
        new("route_not_found", $"Route '{routeId}' was not found.");
    public static DriverError WeakPassword() =>
        new("weak_password", "Password must be at least 8 characters long.");

    public static DriverError ValidationFailed(string message) =>
        new("validation_failed", message);

    public static DriverError DriverNotFound(string driverId) =>
        new("driver_not_found", $"Driver '{driverId}' was not found.");

    public static DriverError InvalidCredentials() =>
        new("invalid_credentials", "Invalid phone number or password.");

    public static DriverError AccountLockedOut(int remainingMinutes) =>
        new("account_locked_out", $"Account is temporarily locked due to multiple failed login attempts. Please try again in {remainingMinutes} minute(s).");

    public static DriverError DriverNotApproved() =>
        new("driver_not_approved", "Driver account is not activated. Please wait for admin approval.");

    public static DriverError DriverRejected(string? reason) =>
        new("driver_rejected", string.IsNullOrWhiteSpace(reason)
            ? "Your driver registration was rejected."
            : $"Your driver registration was rejected. Reason: {reason}");

    public static DriverError DriverSuspended() =>
        new("driver_suspended", "Driver account is suspended. Please contact support.");
    public static DriverError DriverNotFound(Guid driverId) =>
        new("driver_not_found", $"Driver '{driverId}' was not found.");

    public static DriverError PhoneAlreadyInUse() =>
        new("phone_already_in_use", "This phone number is already in use by another driver.");

    public static DriverError CannotUpdateSuspendedDriver() =>
        new("cannot_update_suspended", "Cannot update profile of a suspended driver.");

    public static DriverError CannotUpdatePendingDriver() =>
        new("cannot_update_pending", "Cannot update profile of a pending driver. Please wait for approval.");

    // Route Change Errors
    public static DriverError RouteNotFoundForChange(Guid routeId) =>
        new("route_not_found", $"Route '{routeId}' was not found.");

    public static DriverError RouteChangeAlreadyRequested() =>
        new("route_change_already_requested", "You already have a pending route change request. Please wait for admin approval.");

    public static DriverError SameRouteRequested() =>
        new("same_route_requested", "You are already assigned to this route.");

    public static DriverError CannotRequestRouteChangeForSuspendedDriver() =>
        new("cannot_request_route_change", "Cannot request route change for a suspended driver.");
    public static DriverError CannotRequestRouteChangeForPendingDriver() =>
        new("cannot_request_route_change", "Cannot request route change for a pending driver.");
    public static DriverError ShiftNotFoundForSummary(Guid shiftId) =>
        new("shift_not_found", $"Shift '{shiftId}' was not found.");
    public static DriverError ShiftNotFoundForTrip(Guid shiftId) =>
        new("shift_not_found", $"Shift '{shiftId}' was not found.");

    public static DriverError ShiftNotActive() =>
        new("shift_not_active", "Shift is not active. Please start a shift first.");

    public static DriverError TripAlreadyActive() =>
        new("trip_already_active", "You already have an active trip. End the current trip before starting a new one.");

    public static DriverError NoActiveTrip() =>
        new("no_active_trip", "No active trip found. Start a new trip first.");

    public static DriverError CannotCompleteTripWithPassengers() =>
        new("cannot_complete_trip", "Cannot complete trip while passengers are still on board. All passengers must alight first.");

    public static DriverError CannotCompleteTripWithPassengers(int count) =>
        new("cannot_complete_trip", $"Trip cannot end because {count} passenger(s) are still on board. All passengers must alight before the trip can end.");

    public static DriverError InvalidDirection(string direction) =>
        new("invalid_direction", $"Invalid direction format '{direction}'. Use format like 'A→F'.");

    public static DriverError RouteNotFoundForTrip() =>
        new("route_not_found", "Route not found for this shift.");

    public static DriverError DriverNotAtDestination(string node, double distance) =>
        new("driver_not_at_destination", $"Driver is not at destination node '{node}'. Current distance: {distance:F2} km.");

    public static DriverError DestinationNodeNotFound(string node) =>
        new("destination_node_not_found", $"Destination node '{node}' not found on this route.");

    public static DriverError CannotCompleteTrip(string message) =>
        new("cannot_complete_trip", message);

    public static DriverError JourneyNotFound(Guid journeyId) =>
        new("journey_not_found", $"Passenger journey '{journeyId}' was not found.");

    public static DriverError Forbidden(string message = "You do not have permission to access or modify this driver resource.") =>
        new("forbidden", message);
}
