namespace TaxiPayApi.Application.DTOs.Passengers;

public record PassengerLoginResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime TokenExpiresAt { get; init; }
    public DateTime LoginTime { get; init; }
    public PassengerResponse Passenger { get; init; } = null!;
}