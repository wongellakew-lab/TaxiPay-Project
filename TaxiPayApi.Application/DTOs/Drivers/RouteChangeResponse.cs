namespace TaxiPayApi.Application.DTOs.Drivers;

public record RouteChangeResponse
{
    public Guid RequestId { get; init; }
    public Guid DriverId { get; init; }
    public string DriverName { get; init; } = string.Empty;
    public RouteSummaryDto? CurrentRoute { get; init; }
    public RouteSummaryDto? RequestedRoute { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime RequestedAt { get; init; }
    public DateTime? ReviewedAt { get; init; }
    public string? RejectionReason { get; init; }
}