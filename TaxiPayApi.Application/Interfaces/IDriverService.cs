using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Drivers;

namespace TaxiPayApi.Application.Interfaces;

public interface IDriverService
{
    // Registration
    Task<Result<DriverResponse, DriverError>> CreateDriverAsync(
        CreateDriverRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<LoginResponse, DriverError>> LoginAsync(
        DriverLoginRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<DriverResponse, DriverError>> GetProfileAsync(
        Guid driverId,
        CancellationToken cancellationToken = default);

    Task<Result<DriverResponse, DriverError>> UpdateProfileAsync(
        Guid driverId,
        UpdateProfileRequest request,
        CancellationToken cancellationToken = default);
    Task<Result<RouteChangeResponse, DriverError>> RequestRouteChangeAsync(
        Guid driverId,
        RequestRouteChangeRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<RouteChangeResponse?, DriverError>> GetCurrentRouteChangeRequestAsync(
        Guid driverId,
        CancellationToken cancellationToken = default);
    // Dashboard & Statistics
    Task<Result<DriverDashboardResponse, DriverError>> GetDashboardAsync(
        Guid driverId,
        CancellationToken cancellationToken = default);

    Task<Result<ShiftSummaryResponse, DriverError>> GetShiftSummaryAsync(
        Guid shiftId,
        CancellationToken cancellationToken = default);
    // Trip Management
    Task<Result<TripResponse, DriverError>> StartNewTripAsync(
        Guid shiftId,
        string direction,
        CancellationToken cancellationToken = default);

    Task<Result<TripResponse, DriverError>> CompleteCurrentTripAsync(
        Guid shiftId,
        decimal driverLatitude,
        decimal driverLongitude,
        CancellationToken cancellationToken = default);

    Task<Result<ActivePassengerDto, DriverError>> RequestPassengerAlightingAsync(
        Guid journeyId,
        CancellationToken cancellationToken = default);

    Task<Result<ActivePassengerDto, DriverError>> ConfirmPassengerAlightingAsync(
        Guid journeyId,
        CancellationToken cancellationToken = default);
}