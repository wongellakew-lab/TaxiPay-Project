using System.Text.Json.Serialization;

namespace TaxiPayApi.Domain.Entities;

public class PassengerRouteRequest : BaseEntity
{
    public Guid PassengerId { get; set; }
    public required string FromLocation { get; set; }
    public required string ToLocation { get; set; }
    public string Description { get; set; } = string.Empty;
    public PassengerRouteRequestStatus Status { get; set; } = PassengerRouteRequestStatus.Pending;
    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public string? AdminNotes { get; set; }
    public Guid? ReviewedByAdminId { get; set; }

    // Navigation Properties
    public Passenger? Passenger { get; set; }
    public Admin? ReviewedByAdmin { get; set; }
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum PassengerRouteRequestStatus
{
    Pending,
    Approved,
    Rejected
}
