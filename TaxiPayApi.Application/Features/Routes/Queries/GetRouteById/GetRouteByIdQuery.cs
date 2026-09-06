using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.Common.Configuration;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Routes.Queries.GetRouteById;

public record GetRouteByIdQuery(Guid Id) : IRequest<RouteDetailDto?>;

public class GetRouteByIdQueryHandler(
    IRouteService _routeService,
    HybridCache _cache,
    IOptions<CacheSettings> _cacheSettings)
    : IRequestHandler<GetRouteByIdQuery, RouteDetailDto?>
{
    public async Task<RouteDetailDto?> Handle(GetRouteByIdQuery request, CancellationToken cancellationToken)
    {
        var entryOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(_cacheSettings.Value.RouteExpirationMinutes),
            LocalCacheExpiration = TimeSpan.FromMinutes(_cacheSettings.Value.RouteExpirationMinutes)
        };

        return await _cache.GetOrCreateAsync(
            CacheKeys.RouteById(request.Id),
            async ct => await _routeService.GetRouteByIdAsync(request.Id, ct),
            entryOptions,
            tags: [CacheKeys.TagRoutes],
            cancellationToken: cancellationToken);
    }
}

