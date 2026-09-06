using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Admins.Commands.ToggleRouteStatus;

public record ToggleRouteStatusCommand(Guid RouteId) : IRequest<bool>;

public class ToggleRouteStatusCommandHandler(IAdminService _adminService, HybridCache _cache)
    : IRequestHandler<ToggleRouteStatusCommand, bool>
{
    public async Task<bool> Handle(ToggleRouteStatusCommand request, CancellationToken cancellationToken)
    {
        var success = await _adminService.ToggleRouteStatusAsync(request.RouteId, cancellationToken);
        if (success)
        {
            await _cache.RemoveByTagAsync(CacheKeys.TagRoutes, cancellationToken);
            await _cache.RemoveAsync(CacheKeys.RoutesActive, cancellationToken);
            await _cache.RemoveAsync(CacheKeys.RoutesAll, cancellationToken);
            await _cache.RemoveAsync(CacheKeys.RouteById(request.RouteId), cancellationToken);
        }
        return success;
    }
}

