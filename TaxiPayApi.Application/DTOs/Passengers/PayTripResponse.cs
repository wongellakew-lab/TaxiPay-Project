namespace TaxiPayApi.Application.DTOs.Passengers;

public record PayTripResponse
{
    public Guid TripId { get; init; }
    public Guid PassengerId { get; init; }
    public string DriverId { get; init; } = string.Empty;
    public string DriverName { get; init; } = string.Empty;
    public string RouteName { get; init; } = string.Empty;
    public string BoardingNode { get; init; } = string.Empty;
    public string DestinationNode { get; init; } = string.Empty;
    public decimal Fare { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public string PaymentStatus { get; init; } = string.Empty;
    public DateTime PaidAt { get; init; }
    public string ReceiptNumber { get; init; } = string.Empty;
    public decimal? NewBalance { get; init; }
}