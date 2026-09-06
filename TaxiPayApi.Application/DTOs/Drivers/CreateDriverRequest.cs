namespace TaxiPayApi.Application.DTOs.Drivers;

public record CreateDriverRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string LicenseNumber { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
    public Guid? PreferredRouteId { get; init; }
}