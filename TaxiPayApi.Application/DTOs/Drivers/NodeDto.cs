namespace TaxiPayApi.Application.DTOs.Drivers;

public record NodeDto
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
}