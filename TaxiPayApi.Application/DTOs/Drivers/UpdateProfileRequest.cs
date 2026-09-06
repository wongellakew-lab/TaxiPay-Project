namespace TaxiPayApi.Application.DTOs.Drivers;

public record UpdateProfileRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string? PhotoUrl { get; init; }
}