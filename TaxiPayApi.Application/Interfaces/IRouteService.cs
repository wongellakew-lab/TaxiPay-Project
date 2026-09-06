using TaxiPayApi.Application.DTOs.Admin;

namespace TaxiPayApi.Application.Interfaces;

public interface IRouteService
{
    Task<List<RouteDetailDto>> GetActiveRoutesAsync(CancellationToken ct = default);
    Task<RouteDetailDto?> GetRouteByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<FareMatrixDto>> GetFaresForRouteAsync(Guid routeId, CancellationToken ct = default);
}
