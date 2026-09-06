using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Admins.Commands.DeleteRoute;

public record DeleteRouteCommand(Guid RouteId) : IRequest<bool>;

public class DeleteRouteCommandHandler(IAdminService _adminService, HybridCache _cache)
    : IRequestHandler<DeleteRouteCommand, bool>
{
    public async Task<bool> Handle(DeleteRouteCommand request, CancellationToken cancellationToken)
    {
        var success = await _adminService.DeleteRouteAsync(request.RouteId, cancellationToken);
        if (success)
        {
            await _cache.RemoveByTagAsync(CacheKeys.TagRoutes, cancellationToken);
            await _cache.RemoveByTagAsync(CacheKeys.TagFares, cancellationToken);
            await _cache.RemoveAsync(CacheKeys.RoutesActive, cancellationToken);
            await _cache.RemoveAsync(CacheKeys.RoutesAll, cancellationToken);
            await _cache.RemoveAsync(CacheKeys.RouteById(request.RouteId), cancellationToken);
            await _cache.RemoveAsync(CacheKeys.FaresForRoute(request.RouteId), cancellationToken);
        }
        return success;
    }
}

