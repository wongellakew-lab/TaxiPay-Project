namespace TaxiPayApi.Application.DTOs.Drivers;

public record ActiveTripDto
{
    public Guid TripId { get; init; }
    public int TripNumber { get; init; }
    public string RouteName { get; init; } = string.Empty;
    public string RouteCode { get; init; } = string.Empty;
    public string Direction { get; init; } = string.Empty;
    public string DirectionDisplay { get; init; } = string.Empty;
    public DateTime TripStart { get; init; }
    public string Status { get; init; } = string.Empty;
    public int PassengersCount { get; init; }
    public decimal TripRevenue { get; init; }
}
