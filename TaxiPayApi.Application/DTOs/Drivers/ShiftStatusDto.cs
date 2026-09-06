namespace TaxiPayApi.Application.DTOs.Drivers;

public record ShiftStatusDto
{
    public Guid ShiftId { get; init; }
    public DateTime StartTime { get; init; }
    public int CurrentTripNumber { get; init; }
    public decimal TotalRevenue { get; init; }
    public int TotalPassengers { get; init; }
    public bool IsActive { get; init; }
}