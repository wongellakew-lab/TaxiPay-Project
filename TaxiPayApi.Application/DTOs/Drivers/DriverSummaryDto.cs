
using TaxiPayApi.Domain.Entities;

namespace TaxiPayApi.Application.DTOs.Drivers;

public record DriverSummaryDto
{
    public Guid Id { get; init; }
    public string DriverId { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string? PhotoUrl { get; init; }
    public DriverStatus Status { get; init; }
    public decimal Rating { get; init; }
    public string? RouteName { get; init; }
    public string? RouteCode { get; init; }
    public List<NodeDto> RouteNodes { get; init; } = new();
}