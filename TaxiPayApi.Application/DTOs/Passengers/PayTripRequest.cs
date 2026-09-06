using TaxiPayApi.Domain.Entities;

namespace TaxiPayApi.Application.DTOs.Passengers;

public record PayTripRequest
{
    public string DriverId { get; init; } = string.Empty;
    public string BoardingNode { get; init; } = string.Empty;
    public string DestinationNode { get; init; } = string.Empty;
    public PaymentMethod PaymentMethod { get; init; }
}