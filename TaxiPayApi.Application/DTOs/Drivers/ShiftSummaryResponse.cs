namespace TaxiPayApi.Application.DTOs.Drivers;

public record ShiftSummaryResponse
{
    public Guid ShiftId { get; init; }
    public DateTime ShiftStart { get; init; }
    public DateTime? ShiftEnd { get; init; }
    public string Status { get; init; } = string.Empty;
    public int TotalPassengers { get; init; }
    public decimal TotalRevenue { get; init; }
    public int TotalTrips { get; init; }
    public decimal AverageFare { get; init; }
    public PaymentBreakdownDto PaymentBreakdown { get; init; } = null!;
    public List<TripSummaryDto> Trips { get; init; } = new();
}