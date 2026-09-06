namespace TaxiPayApi.Application.DTOs.Drivers;

public record DriverLoginRequest
{
    public string Phone { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}