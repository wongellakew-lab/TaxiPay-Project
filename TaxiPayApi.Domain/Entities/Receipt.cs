namespace TaxiPayApi.Domain.Entities;

public class Receipt : BaseEntity
{
    public Guid TripId { get; set; }
    public Guid PassengerId { get; set; }
    public Guid? JourneyId { get; set; }

    public string ReceiptNumber { get; set; } = string.Empty;
    public string PassengerPhone { get; set; } = string.Empty;
    public string PassengerName { get; set; } = string.Empty;
    public string DriverName { get; set; } = string.Empty;
    public string DriverCode { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public string BoardingNode { get; set; } = string.Empty;
    public string DestinationNode { get; set; } = string.Empty;
    public decimal Fare { get; set; }
    public string PaymentMethod { get; set; } = string.Empty;
    public string PaymentStatus { get; set; } = string.Empty;
    public string TransactionRef { get; set; } = string.Empty;
    public string ReceiptText { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public ReceiptDeliveryStatus DeliveryStatus { get; set; } = ReceiptDeliveryStatus.Sent;

    // Navigation Properties
    public Trip? Trip { get; set; }
    public Passenger? Passenger { get; set; }
    public PassengerJourney? Journey { get; set; }
}

public enum ReceiptDeliveryStatus
{
    Sent,
    Failed
}