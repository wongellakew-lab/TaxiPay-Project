namespace TaxiPayApi.Domain.Entities;

public class Trip : BaseEntity
{
    // Core identifiers
    public Guid DriverShiftId { get; set; }
    public Guid RouteId { get; set; }
    public Guid DriverId { get; set; }

    // Trip details
    public string Direction { get; set; } = string.Empty;
    public int TripNumber { get; set; }

    public DateTime TripStart { get; set; }
    public DateTime? TripEnd { get; set; }

    // Completion tracking (Decision #11)
    public bool HasReachedFinalDestination { get; set; }

    // Aggregated totals
    public int PassengersCount { get; set; }
    public decimal TripRevenue { get; set; }

    public TripStatus Status { get; set; }

    // Navigation Properties
    public DriverShift? DriverShift { get; set; }
    public Route? Route { get; set; }
    public Driver? Driver { get; set; }
    public ICollection<PassengerJourney> PassengerJourneys { get; set; } = new List<PassengerJourney>();
    public Receipt? Receipt { get; set; }
    public Dispute? Dispute { get; set; }
}

public enum TripStatus
{
    Active,      // Trip in progress
    Completed,   // All passengers alighted + reached destination
    Disputed     // Under dispute
}