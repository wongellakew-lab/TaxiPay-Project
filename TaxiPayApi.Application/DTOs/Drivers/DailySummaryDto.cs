namespace TaxiPayApi.Application.DTOs.Drivers;

public record DailySummaryDto
{
    public int TotalTrips { get; init; }
    public int TotalPassengers { get; init; }
    public decimal TotalRevenue { get; init; }
    public decimal AverageFare { get; init; }
    public PaymentBreakdownDto PaymentBreakdown { get; init; } = null!;
}