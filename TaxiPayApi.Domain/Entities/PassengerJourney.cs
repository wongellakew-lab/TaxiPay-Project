namespace TaxiPayApi.Domain.Entities;

public enum AlightingStatus
{
    OnBoard,
    AwaitingConfirmation,
    Alighted
}

public class PassengerJourney : BaseEntity
{
    public Guid TripId { get; set; }
    public Guid PassengerId { get; set; }

    public string BoardingNode { get; set; } = string.Empty;
    public string DestinationNode { get; set; } = string.Empty;
    public decimal Fare { get; set; }

    public PaymentMethod PaymentMethod { get; set; }
    public PaymentStatus PaymentStatus { get; set; }
    public AlightingStatus AlightingStatus { get; set; } = AlightingStatus.OnBoard;
    public DateTime? AlightedAt { get; set; }
    public string? TransactionRef { get; set; }

    public bool ReceiptSent { get; set; }

    // Navigation Properties
    public Trip? Trip { get; set; }
    public Passenger? Passenger { get; set; }
}

public enum PaymentMethod
{
    Telebirr,
    Wallet,
    Cash
}

// PaymentStatus
public enum PaymentStatus
{
    Pending,
    Completed,
    Failed,
    Refunded
}