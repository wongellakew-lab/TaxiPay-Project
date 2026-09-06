namespace TaxiPayApi.Domain.Entities;

public class Passenger : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public TaxiPayUser? User { get; set; }

    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    public int TotalTrips { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal Rating { get; set; }

    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }

    // Navigation Properties
    public ICollection<PassengerJourney> PassengerJourneys { get; set; } = new List<PassengerJourney>();
    public ICollection<Dispute> Disputes { get; set; } = new List<Dispute>();
    public ICollection<PassengerRouteRequest> RouteRequests { get; set; } = new List<PassengerRouteRequest>();
    public decimal WalletBalance { get; set; }
}