namespace TaxiPayApi.Application.DTOs.Drivers;

public record ActivePassengerDto
{
    public Guid JourneyId { get; init; }
    public Guid PassengerId { get; init; }
    public string PassengerName { get; init; } = string.Empty;
    public string PassengerPhone { get; init; } = string.Empty;
    public string BoardingNode { get; init; } = string.Empty;
    public string DestinationNode { get; init; } = string.Empty;
    public decimal Fare { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public string PaymentStatus { get; init; } = string.Empty;
    public string AlightingStatus { get; init; } = "OnBoard";
    public DateTime BoardedAt { get; init; }
}