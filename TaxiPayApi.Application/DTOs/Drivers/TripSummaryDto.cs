namespace TaxiPayApi.Application.DTOs.Drivers;

public record TripSummaryDto
{
    public Guid TripId { get; init; }
    public int TripNumber { get; init; }
    public string Direction { get; init; } = string.Empty;
    public DateTime TripStart { get; init; }
    public DateTime? TripEnd { get; init; }
    public int PassengersCount { get; init; }
    public decimal TripRevenue { get; init; }
    public string Status { get; init; } = string.Empty;
}