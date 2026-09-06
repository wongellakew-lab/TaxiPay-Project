using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Admins.Commands.UpdateRoute;

public record UpdateRouteCommand(Guid RouteId, UpdateRouteRequest Request) : IRequest<RouteDetailDto?>;

public class UpdateRouteCommandHandler(IAdminService _adminService, HybridCache _cache)
    : IRequestHandler<UpdateRouteCommand, RouteDetailDto?>
{
    public async Task<RouteDetailDto?> Handle(UpdateRouteCommand request, CancellationToken cancellationToken)
    {
        var result = await _adminService.UpdateRouteAsync(request.RouteId, request.Request, cancellationToken);
        if (result != null)
        {
            await _cache.RemoveByTagAsync(CacheKeys.TagRoutes, cancellationToken);
            await _cache.RemoveAsync(CacheKeys.RoutesActive, cancellationToken);
            await _cache.RemoveAsync(CacheKeys.RoutesAll, cancellationToken);
            await _cache.RemoveAsync(CacheKeys.RouteById(request.RouteId), cancellationToken);
        }
        return result;
    }
}

