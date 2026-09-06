namespace TaxiPayApi.Application.DTOs.Passengers;

public record LoginRequest
{
    public string Phone { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;
}