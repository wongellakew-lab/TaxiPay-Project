using System.Text.Json.Serialization;

namespace TaxiPayApi.Domain.Entities;

public class Route : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string RouteCode { get; set; } = string.Empty;

    // JSONB in PostgreSQL
    public List<Node> Nodes { get; set; } = new List<Node>();

    public bool IsActive { get; set; }

    // Navigation Properties
    public ICollection<FareMatrix> FareMatrices { get; set; } = new List<FareMatrix>();
    public ICollection<DriverShift> Shifts { get; set; } = new List<DriverShift>();
    public ICollection<Trip> Trips { get; set; } = new List<Trip>();
    public ICollection<RouteChangeRequest> CurrentRouteChangeRequests { get; set; } = new List<RouteChangeRequest>();
    public ICollection<RouteChangeRequest> NewRouteChangeRequests { get; set; } = new List<RouteChangeRequest>();
}

public class Node
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}