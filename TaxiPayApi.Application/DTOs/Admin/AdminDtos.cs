using System.Text.Json.Serialization;
using TaxiPayApi.Domain.Entities;

namespace TaxiPayApi.Application.DTOs.Admin;

// Auth DTOs
public record AdminLoginRequest(string Email, string Password);

public record AdminLoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTime TokenExpiresAt,
    AdminProfileResponse Admin
);

public record AdminProfileResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string Phone,
    string Role,
    bool IsActive,
    DateTime? LastLoginAt
);

// Driver Management DTOs
public record AdminDriverListItemDto(
    Guid Id,
    string? DriverId,
    string FirstName,
    string LastName,
    string Phone,
    string LicenseNumber,
    string Status,
    bool IsVerified,
    decimal Rating,
    int TotalTrips,
    decimal TotalRevenue,
    Guid? CurrentRouteId,
    string? CurrentRouteCode,
    string? CurrentRouteName,
    Guid? PreferredRouteId,
    string? PreferredRouteCode,
    string? PreferredRouteName,
    string? RejectionReason,
    DateTime CreatedAt
);

public record UpdateDriverStatusRequest(string Status, string? Note);

public record ApproveDriverRequest(Guid? AssignedRouteId);

public record RejectDriverRequest(string RejectionReason);

public record AssignDriverRouteRequest(Guid RouteId);

// Route Management DTOs
public record AdminNodeDto(
    string Id,
    string Name,
    double Latitude,
    double Longitude
);

public record CreateRouteRequest(
    string Name,
    string RouteCode,
    List<AdminNodeDto> Nodes,
    bool IsActive = true
);

public record UpdateRouteRequest(
    string Name,
    string RouteCode,
    List<AdminNodeDto> Nodes,
    bool IsActive
);

public record RouteDetailDto(
    Guid Id,
    string Name,
    string RouteCode,
    List<AdminNodeDto> Nodes,
    bool IsActive,
    int ActiveDriversCount,
    DateTime CreatedAt
);

// Fare Management DTOs
public record FareMatrixDto(
    Guid Id,
    Guid RouteId,
    string RouteCode,
    string FromNode,
    string ToNode,
    decimal Fare,
    bool IsActive
);

public record CreateFareMatrixRequest(
    Guid RouteId,
    string FromNode,
    string ToNode,
    decimal Fare,
    bool IsActive = true
);

public record UpdateFareMatrixRequest(
    decimal Fare,
    bool IsActive
);

public record GenerateSymmetricFareRequest(
    Guid RouteId,
    decimal BaseFarePerHop
);

// Route Change Request DTOs
public record AdminRouteChangeRequestDto(
    Guid Id,
    Guid DriverId,
    string DriverName,
    string? DriverCode,
    string DriverPhone,
    Guid CurrentRouteId,
    string CurrentRouteCode,
    string CurrentRouteName,
    Guid NewRouteId,
    string NewRouteCode,
    string NewRouteName,
    string Reason,
    string Status,
    bool HasActiveTrip,
    DateTime RequestedAt,
    DateTime? ReviewedAt = null,
    string? RejectionReason = null,
    string? ReviewedByAdminName = null
);

public record ReviewRouteChangeRequestDto(
    bool Approved,
    string? Note
);

// Dispute Resolution DTOs
public record AdminDisputeDto(
    Guid Id,
    Guid TripId,
    string TripNumber,
    Guid PassengerId,
    string PassengerName,
    string PassengerPhone,
    Guid DriverId,
    string DriverName,
    string DriverPhone,
    string RouteCode,
    string RouteName,
    string FromNode,
    string ToNode,
    decimal FarePaid,
    string DisputeType,
    string Description,
    string Status,
    string? ResolutionNotes,
    DateTime? ResolvedAt,
    string? ResolvedByAdminName,
    DateTime CreatedAt
);

public record ReviewDisputeRequestDto(
    string Status, // Open, Closed, Resolved, Rejected
    string ResolutionNotes,
    bool IssueRefund = false
);

public record OpenDisputeRequestDto(
    string? Notes
);

public record CloseDisputeRequestDto(
    string ResolutionNotes,
    bool IssueRefund = false
);

public record AdminAnalyticsOverviewDto(
    decimal TotalRevenue,
    decimal TodayRevenue,
    int TotalTrips,
    int ActiveTrips,
    int TotalDrivers,
    int ActiveDriversOnShift,
    int PendingDriverApprovals,
    int TotalPassengers,
    int OpenDisputes,
    int PendingRouteRequests,
    List<DailyRevenueStatDto> DailyRevenueHistory,
    List<RecentActivityDto> RecentActivities,
    List<CorridorStatDto> CorridorStats
);

public record CorridorStatDto(
    string RouteCode,
    string RouteName,
    decimal TotalRevenue,
    double RevenueSharePercent,
    int ActiveDriversCount,
    int TotalTripsCount,
    string? PeakStation
);

public record DailyRevenueStatDto(
    string Date,
    decimal TotalRevenue,
    int TripsCount
);

public record RecentActivityDto(
    string Id,
    string Type, // "Trip", "DriverRegistration", "Dispute", "RouteChange"
    string Title,
    string Description,
    DateTime Timestamp,
    string Status
);
