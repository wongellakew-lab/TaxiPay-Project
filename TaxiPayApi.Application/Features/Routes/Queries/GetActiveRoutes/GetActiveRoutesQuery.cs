using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.Common.Configuration;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Routes.Queries.GetActiveRoutes;

public record GetActiveRoutesQuery : IRequest<List<RouteDetailDto>>;

public class GetActiveRoutesQueryHandler(
    IRouteService _routeService,
    HybridCache _cache,
    IOptions<CacheSettings> _cacheSettings)
    : IRequestHandler<GetActiveRoutesQuery, List<RouteDetailDto>>
{
    public async Task<List<RouteDetailDto>> Handle(GetActiveRoutesQuery request, CancellationToken cancellationToken)
    {
        var entryOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(_cacheSettings.Value.RouteExpirationMinutes),
            LocalCacheExpiration = TimeSpan.FromMinutes(_cacheSettings.Value.RouteExpirationMinutes)
        };

        return await _cache.GetOrCreateAsync(
            CacheKeys.RoutesActive,
            async ct => await _routeService.GetActiveRoutesAsync(ct),
            entryOptions,
            tags: [CacheKeys.TagRoutes],
            cancellationToken: cancellationToken);
    }
}

