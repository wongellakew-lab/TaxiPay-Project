using System.Text.Json.Serialization;

namespace TaxiPayApi.Domain.Entities;

public class Admin : BaseEntity
{
    public string UserId { get; set; } = string.Empty;
    public TaxiPayUser? User { get; set; }

    public required string FirstName { get; set; }
    public required string LastName { get; set; }
    public required string Email { get; set; }
    public required string Phone { get; set; }
    public AdminRole Role { get; set; }
    public bool IsActive { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public ICollection<RouteChangeRequest> ReviewedRouteChangeRequests { get; set; } = new List<RouteChangeRequest>();
    public ICollection<Dispute> ResolvedDisputes { get; set; } = new List<Dispute>();
}

[JsonConverter(typeof(JsonStringEnumConverter))]
public enum AdminRole
{
    SuperAdmin,
    FareManager,
    RouteManager,
    DisputeResolver
}