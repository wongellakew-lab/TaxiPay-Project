namespace TaxiPayApi.Application.DTOs.Drivers;

public record CompleteTripRequest
{
    public decimal Latitude { get; init; }
    public decimal Longitude { get; init; }
}