using TaxiPayApi.Application.DTOs.Drivers;

namespace TaxiPayApi.Application.DTOs.Passengers;

public record DriverByCodeResponse
{
    public string DriverId { get; init; } = string.Empty;
    public string FullName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public decimal Rating { get; init; }
    public string? PhotoUrl { get; init; }
    public string RouteName { get; init; } = string.Empty;
    public string RouteCode { get; init; } = string.Empty;
    public List<NodeDto> RouteNodes { get; init; } = new();
    public bool IsActive { get; init; }
    public bool HasActiveShift { get; init; }
    public bool HasActiveTrip { get; init; }
    public Guid? ActiveTripId { get; init; }
    public string? ActiveTripDirection { get; init; }
    public string? ActiveTripDirectionDisplay { get; init; }
}