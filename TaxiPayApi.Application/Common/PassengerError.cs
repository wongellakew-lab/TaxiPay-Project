namespace TaxiPayApi.Application.Common;

public sealed record PassengerError(string Code, string Message)
{
    // Profile Errors
    public static PassengerError PassengerNotFound(Guid passengerId) =>
        new("passenger_not_found", $"Passenger '{passengerId}' was not found.");

    // Driver Errors
    public static PassengerError DriverNotFound(string driverId) =>
        new("driver_not_found", $"Driver '{driverId}' was not found.");

    // Route Errors
    public static PassengerError RouteNotFound(Guid routeId) =>
        new("route_not_found", $"Route '{routeId}' was not found.");

    public static PassengerError InvalidNodes(string fromNode, string toNode) =>
        new("invalid_nodes", $"Invalid nodes: '{fromNode}' or '{toNode}' not on this route.");

    public static PassengerError FareNotFound(string fromNode, string toNode) =>
        new("fare_not_found", $"No fare found for {fromNode} → {toNode}.");

    // Authentication Errors
    public static PassengerError PhoneAlreadyRegistered() =>
        new("phone_already_registered", "This phone number is already registered.");

    public static PassengerError InvalidCredentials() =>
        new("invalid_credentials", "Invalid phone number or password.");

    public static PassengerError AccountLockedOut(int remainingMinutes) =>
        new("account_locked_out", $"Account is temporarily locked due to multiple failed login attempts. Please try again in {remainingMinutes} minute(s).");

    public static PassengerError ValidationFailed(string message) =>
        new("validation_failed", message);

    // Payment Errors
    public static PassengerError DriverNotActive() =>
        new("driver_not_active", "Driver is not active. Please try another driver.");

    public static PassengerError DriverNotOnShift() =>
        new("driver_not_on_shift", "Driver is not currently on shift. Please try another driver.");

    public static PassengerError DriverNoActiveTrip() =>
        new("driver_no_active_trip", "Driver does not currently have an active trip. Please wait until the driver starts a trip.");

    public static PassengerError TripNotActive() =>
        new("trip_not_active", "This trip has ended and is no longer accepting passengers.");

    public static PassengerError InsufficientBalance() =>
        new("insufficient_balance", "Insufficient wallet balance. Please add funds.");

    public static PassengerError InvalidPaymentMethod() =>
        new("invalid_payment_method", "Invalid payment method. Choose Telebirr or Wallet.");

    // Profile Update Errors
    public static PassengerError PhoneAlreadyInUse() =>
        new("phone_already_in_use", "This phone number is already registered. Please use a different phone number.");

    public static PassengerError InvalidPhone() =>
        new("invalid_phone", "Please provide a valid Ethiopian phone number (e.g. 0912345678 or 0712345678).");

    // Journey Errors
    public static PassengerError JourneyNotFound(Guid journeyId) =>
        new("journey_not_found", $"Passenger journey '{journeyId}' was not found.");

    public static PassengerError UnauthorizedPassenger() =>
        new("unauthorized_passenger", "This journey does not belong to the current passenger.");

    public static PassengerError Forbidden(string message = "You do not have permission to access or modify this passenger resource.") =>
        new("forbidden", message);
}