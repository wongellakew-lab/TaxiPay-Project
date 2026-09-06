using System.Text.Json.Serialization;

namespace TaxiPayApi.Domain.Entities;

public class Dispute : BaseEntity
{
    public Guid TripId { get; set; }
    public Guid PassengerId { get; set; }
    public Guid DriverId { get; set; }
    public DisputeType Type { get; set; }
    public string Description { get; set; } = string.Empty;
    public DisputeStatus Status { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public Guid? ResolvedByAdminId { get; set; }

    // Navigation Properties
    public Trip? Trip { get; set; }
    public Passenger? Passenger { get; set; }
    public Driver? Driver { get; set; }
    public Admin? ResolvedByAdmin { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DisputeType
{
    Overcharged,
    WrongDropoff,
    NonPayment,
    Other
}
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DisputeStatus
{
    Open,
    Reviewing,
    Closed,
    Resolved,
    Rejected
}