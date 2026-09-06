namespace TaxiPayApi.Application.DTOs.Passengers;

public record FavoriteDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string FromNode { get; init; } = string.Empty;
    public string ToNode { get; init; } = string.Empty;
    public decimal Fare { get; init; }
    public string RouteName { get; init; } = string.Empty;
    public string RouteCode { get; init; } = string.Empty;
}