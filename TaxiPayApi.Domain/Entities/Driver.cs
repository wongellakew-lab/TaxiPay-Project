using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace TaxiPayApi.Domain.Entities;

public class Driver : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public TaxiPayUser? User { get; set; }

    public string? DriverId { get; set; }
    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Phone { get; set; }
    public required string LicenseNumber { get; set; }
    public string? PhotoUrl { get; set; }
    public decimal Rating { get; set; }

    public int TotalTrips { get; set; }

    public decimal TotalRevenue { get; set; }
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DriverStatus Status { get; set; }

    public Guid? CurrentRouteId { get; set; }
    public Guid? PreferredRouteId { get; set; }
    public string? RejectionReason { get; set; }

    // Navigation Properties
    public Route? CurrentRoute { get; set; }
    public Route? PreferredRoute { get; set; }
    public ICollection<DriverShift> Shifts { get; set; } = new List<DriverShift>();
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
    public ICollection<RouteChangeRequest> RouteChangeRequests { get; set; } = new List<RouteChangeRequest>();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum DriverStatus
{
    Pending,
    Active,
    Suspended,
    Rejected
}