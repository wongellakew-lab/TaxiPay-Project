namespace TaxiPayApi.Domain.Entities;

public class RouteChangeRequest : BaseEntity
{
    public Guid DriverId { get; set; }
    public Guid CurrentRouteId { get; set; }
    public Guid NewRouteId { get; set; }
    public string Reason { get; set; } = string.Empty;

    public RouteChangeStatus Status { get; set; }
    public DateTime RequestedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? RejectionReason { get; set; }
    public Guid? ReviewedByAdminId { get; set; }

    // Navigation Properties
    public Driver? Driver { get; set; }
    public Route? CurrentRoute { get; set; }
    public Route? NewRoute { get; set; }
    public Admin? ReviewedByAdmin { get; set; }
}

public enum RouteChangeStatus
{
    Pending,
    Approved,
    Rejected
}