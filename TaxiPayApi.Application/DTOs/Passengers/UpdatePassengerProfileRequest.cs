namespace TaxiPayApi.Application.DTOs.Passengers;

public record UpdatePassengerProfileRequest
{
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
}
