namespace TaxiPayApi.Application.Interfaces;

public interface ICurrentUserService
{
    string? UserId { get; }
    string? Role { get; }
    Guid? DriverGuid { get; }
    string? DriverCode { get; }
    Guid? PassengerGuid { get; }
    Guid? AdminGuid { get; }
    bool IsAuthenticated { get; }
    bool IsAdmin { get; }
    bool IsDriver { get; }
    bool IsPassenger { get; }
}
