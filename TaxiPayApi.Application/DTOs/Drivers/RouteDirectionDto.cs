namespace TaxiPayApi.Application.DTOs.Drivers;

public record RouteDirectionDto
{
    public string DirectionKey { get; init; } = string.Empty;
    public string DisplayLabel { get; init; } = string.Empty;
    public string FromNodeId { get; init; } = string.Empty;
    public string FromNodeName { get; init; } = string.Empty;
    public string ToNodeId { get; init; } = string.Empty;
    public string ToNodeName { get; init; } = string.Empty;
    public bool IsForward { get; init; } = true;
}
