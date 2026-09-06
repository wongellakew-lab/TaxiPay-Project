namespace TaxiPayApi.Application.DTOs.Drivers;

public record DriverDashboardResponse
{
    public string DriverId { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string RouteName { get; init; } = string.Empty;
    public string RouteCode { get; init; } = string.Empty;

    // Shift Information
    public Guid? ActiveShiftId { get; init; }
    public Guid? ActiveTripId { get; init; }
    public bool IsTripActive { get; init; }
    public DateTime ShiftStart { get; init; }
    public int CurrentTripNumber { get; init; }

    // Revenue Statistics
    public decimal TotalRevenue { get; init; }
    public int TotalPassengers { get; init; }
    public int AvailableSeats { get; init; }
    public int Capacity { get; init; } = 15; // Fixed capacity

    // Active Passengers
    public List<ActivePassengerDto> ActivePassengers { get; init; } = new();

    // Active & Completed Trip Details
    public ActiveTripDto? ActiveTrip { get; init; }
    public CompletedTripDto? LastCompletedTrip { get; init; }

    // Route Details & Directions
    public List<NodeDto> RouteNodes { get; init; } = new();
    public List<RouteDirectionDto> AvailableDirections { get; init; } = new();

    // Today's Summary
    public DailySummaryDto TodaySummary { get; init; } = null!;
}