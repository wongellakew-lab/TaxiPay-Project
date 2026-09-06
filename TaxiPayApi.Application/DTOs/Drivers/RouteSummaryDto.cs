namespace TaxiPayApi.Application.DTOs.Drivers;

public record RouteSummaryDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string RouteCode { get; init; } = string.Empty;
    public List<NodeDto> Nodes { get; init; } = new();
    public bool IsActive { get; init; }
}