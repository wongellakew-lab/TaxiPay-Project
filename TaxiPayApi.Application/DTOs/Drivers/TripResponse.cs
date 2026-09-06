namespace TaxiPayApi.Application.DTOs.Drivers;

public record TripResponse
{
    public Guid TripId { get; init; }
    public Guid ShiftId { get; init; }
    public Guid DriverId { get; init; }
    public string DriverName { get; init; } = string.Empty;
    public int TripNumber { get; init; }
    public string Direction { get; init; } = string.Empty;
    public string RouteName { get; init; } = string.Empty;
    public DateTime TripStart { get; init; }
    public DateTime? TripEnd { get; init; }
    public int PassengersCount { get; init; }
    public decimal TripRevenue { get; init; }
    public string Status { get; init; } = string.Empty;
}