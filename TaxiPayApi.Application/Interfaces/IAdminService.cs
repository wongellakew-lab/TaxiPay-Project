using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Admin;

namespace TaxiPayApi.Application.Interfaces;

public interface IAdminService
{
    // Auth
    Task<Result<AdminLoginResponse, AdminError>> LoginAsync(AdminLoginRequest request, CancellationToken ct = default);
    Task<AdminProfileResponse?> GetProfileAsync(Guid adminId, CancellationToken ct = default);

    // Driver Management
    Task<List<AdminDriverListItemDto>> GetDriversAsync(string? status = null, CancellationToken ct = default);
    Task<AdminDriverListItemDto?> GetDriverByIdAsync(Guid driverId, CancellationToken ct = default);
    Task<bool> UpdateDriverStatusAsync(Guid driverId, UpdateDriverStatusRequest request, CancellationToken ct = default);
    Task<bool> ApproveDriverAsync(Guid driverId, ApproveDriverRequest request, CancellationToken ct = default);
    Task<bool> RejectDriverAsync(Guid driverId, RejectDriverRequest request, CancellationToken ct = default);
    Task<bool> AssignDriverRouteAsync(Guid driverId, AssignDriverRouteRequest request, CancellationToken ct = default);

    // Route Management
    Task<List<RouteDetailDto>> GetRoutesAsync(CancellationToken ct = default);
    Task<RouteDetailDto?> GetRouteByIdAsync(Guid routeId, CancellationToken ct = default);
    Task<RouteDetailDto> CreateRouteAsync(CreateRouteRequest request, CancellationToken ct = default);
    Task<RouteDetailDto?> UpdateRouteAsync(Guid routeId, UpdateRouteRequest request, CancellationToken ct = default);
    Task<bool> ToggleRouteStatusAsync(Guid routeId, CancellationToken ct = default);
    Task<bool> DeleteRouteAsync(Guid routeId, CancellationToken ct = default);

    // Fare Management
    Task<List<FareMatrixDto>> GetFareMatricesByRouteAsync(Guid routeId, CancellationToken ct = default);
    Task<FareMatrixDto> CreateFareMatrixAsync(CreateFareMatrixRequest request, CancellationToken ct = default);
    Task<FareMatrixDto?> UpdateFareMatrixAsync(Guid id, UpdateFareMatrixRequest request, CancellationToken ct = default);
    Task<List<FareMatrixDto>> GenerateSymmetricFaresAsync(GenerateSymmetricFareRequest request, CancellationToken ct = default);

    // Route Change Requests
    Task<List<AdminRouteChangeRequestDto>> GetRouteChangeRequestsAsync(string? status = null, CancellationToken ct = default);
    Task<bool> ReviewRouteChangeRequestAsync(Guid requestId, Guid adminId, ReviewRouteChangeRequestDto request, CancellationToken ct = default);

    // Dispute Resolution
    Task<List<AdminDisputeDto>> GetDisputesAsync(string? status = null, CancellationToken ct = default);
    Task<bool> OpenDisputeAsync(Guid disputeId, Guid adminId, string? notes, CancellationToken ct = default);
    Task<bool> CloseDisputeAsync(Guid disputeId, Guid adminId, string? resolutionNotes, bool issueRefund, CancellationToken ct = default);
    Task<bool> ResolveDisputeAsync(Guid disputeId, Guid adminId, ReviewDisputeRequestDto request, CancellationToken ct = default);

    // System Monitoring & Analytics
    Task<AdminAnalyticsOverviewDto> GetAnalyticsOverviewAsync(CancellationToken ct = default);
}
