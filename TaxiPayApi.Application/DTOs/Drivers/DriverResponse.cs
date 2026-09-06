

using TaxiPayApi.Domain.Entities;

namespace TaxiPayApi.Application.DTOs.Drivers;

public record DriverResponse
{
    public Guid Id { get; init; }
    public string? DriverId { get; init; }
    public string FirstName { get; init; } = string.Empty;
    public string LastName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public string LicenseNumber { get; init; } = string.Empty;
    public string? PhotoUrl { get; init; }
    public decimal Rating { get; init; }
    public int TotalTrips { get; init; }
    public decimal TotalRevenue { get; init; }
    public DriverStatus Status { get; init; }
    public bool IsVerified { get; init; }
    public DateTime? VerifiedAt { get; init; }
    public DateTime? LastLoginAt { get; init; }
    public Guid? CurrentRouteId { get; init; }
    public string? CurrentRouteName { get; init; }
    public DateTime CreatedAt { get; init; }
}