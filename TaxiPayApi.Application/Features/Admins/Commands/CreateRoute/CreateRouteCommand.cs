using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Admins.Commands.CreateRoute;

public record CreateRouteCommand(CreateRouteRequest Request) : IRequest<RouteDetailDto>;

public class CreateRouteCommandHandler(IAdminService _adminService, HybridCache _cache)
    : IRequestHandler<CreateRouteCommand, RouteDetailDto>
{
    public async Task<RouteDetailDto> Handle(CreateRouteCommand request, CancellationToken cancellationToken)
    {
        var result = await _adminService.CreateRouteAsync(request.Request, cancellationToken);
        if (result != null)
        {
            await _cache.RemoveByTagAsync(CacheKeys.TagRoutes, cancellationToken);
            await _cache.RemoveAsync(CacheKeys.RoutesActive, cancellationToken);
            await _cache.RemoveAsync(CacheKeys.RoutesAll, cancellationToken);
        }
        return result;
    }
}

