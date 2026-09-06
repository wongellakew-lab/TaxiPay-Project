namespace TaxiPayApi.Application.DTOs.Auth;

public record CurrentUserResponse(
    string UserId,
    string Role,
    string FirstName,
    string LastName,
    string? Email,
    string? PhoneNumber,
    Guid? PassengerId = null,
    decimal? WalletBalance = null,
    Guid? DriverId = null,
    string? DriverCode = null,
    string? DriverStatus = null,
    Guid? AdminId = null,
    string? AdminRole = null
);
