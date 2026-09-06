using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Application.DTOs.Passengers;

namespace TaxiPayApi.Application.Services;

public interface IPassengerService
{
    // Authentication
    Task<Result<PassengerResponse, PassengerError>> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);

    Task<Result<PassengerLoginResponse, PassengerError>> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);

    // Profile
    Task<Result<PassengerResponse, PassengerError>> GetProfileAsync(
        Guid passengerId,
        CancellationToken cancellationToken = default);

    Task<Result<PassengerResponse, PassengerError>> UpdateProfileAsync(
        Guid passengerId,
        UpdatePassengerProfileRequest request,
        CancellationToken cancellationToken = default);

    // Dashboard
    Task<Result<PassengerDashboardResponse, PassengerError>> GetDashboardAsync(
        Guid passengerId,
        CancellationToken cancellationToken = default);

    // Trip History
    Task<Result<TripHistoryResponse, PassengerError>> GetTripHistoryAsync(
        Guid passengerId,
        int pageNumber,
        int pageSize,
        CancellationToken cancellationToken = default);

    // Driver
    Task<Result<DriverByCodeResponse, PassengerError>> GetDriverByCodeAsync(
        string driverId,
        CancellationToken cancellationToken = default);

    Task<Result<List<DriverSearchResultDto>, PassengerError>> SearchDriversByCodeAsync(
        string query,
        int limit = 10,
        CancellationToken cancellationToken = default);

    // Fare
    Task<Result<FareResponse, PassengerError>> GetFareAsync(
        Guid routeId,
        string fromNode,
        string toNode,
        CancellationToken cancellationToken = default);

    // Payment
    Task<Result<PayTripResponse, PassengerError>> PayForTripAsync(
        Guid passengerId,
        PayTripRequest request,
        CancellationToken cancellationToken = default);

    // Alighting Confirmation
    Task<Result<ActivePassengerDto, PassengerError>> ConfirmPassengerAlightingAsync(
        Guid passengerId,
        Guid journeyId,
        CancellationToken cancellationToken = default);

    // Route Requests
    Task<Result<PassengerRouteRequestDto, PassengerError>> SubmitRouteRequestAsync(
        Guid passengerId,
        CreatePassengerRouteRequestDto request,
        CancellationToken cancellationToken = default);

    Task<Result<List<PassengerRouteRequestDto>, PassengerError>> GetPassengerRouteRequestsAsync(
        Guid passengerId,
        CancellationToken cancellationToken = default);

    // Wallet Top-Up
    Task<Result<TopUpWalletResponse, PassengerError>> TopUpWalletAsync(
        Guid passengerId,
        TopUpWalletRequest request,
        CancellationToken cancellationToken = default);
}