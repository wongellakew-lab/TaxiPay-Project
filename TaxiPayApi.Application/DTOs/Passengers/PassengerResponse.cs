namespace TaxiPayApi.Application.DTOs.Passengers;

public record PassengerResponse
{
    public Guid Id { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    // FullName is computed by the AutoMapper profile so both GetProfile
    // and UpdateProfile return consistent data that the Angular
    // PassengerProfileResponse interface expects.
    public string FullName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public decimal WalletBalance { get; init; }
    public int TotalTrips { get; init; }
    public decimal TotalSpent { get; init; }
    public decimal Rating { get; init; }
    public bool IsVerified { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public DateTime CreatedAt { get; init; }
}