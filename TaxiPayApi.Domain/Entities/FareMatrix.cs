namespace TaxiPayApi.Domain.Entities;

public class FareMatrix : BaseEntity
{
    public required Guid? RouteId { get; set; }
    public required string FromNode { get; set; } = string.Empty;
    public required string ToNode { get; set; } = string.Empty;
    public decimal Fare { get; set; }
    public bool IsActive { get; set; }
    public Route? Route { get; set; }
}