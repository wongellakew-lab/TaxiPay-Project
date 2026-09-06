using System.Text.Json.Serialization;

namespace TaxiPayApi.Domain.Entities;

public class DriverShift : BaseEntity
{
    public Guid DriverId { get; set; }
    public Guid RouteId { get; set; }

    public DateTime ShiftStart { get; set; }
    public DateTime? ShiftEnd { get; set; }

    // Trip tracking
    public int CurrentTripNumber { get; set; }
    public Guid? CurrentTripId { get; set; }

    // Aggregated totals
    public int TotalPassengers { get; set; }
    public decimal TotalRevenue { get; set; }

    public ShiftStatus Status { get; set; }

    // Navigation Properties
    public Driver? Driver { get; set; }
    public Route? Route { get; set; }
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum ShiftStatus
{
    Active,
    Completed
}