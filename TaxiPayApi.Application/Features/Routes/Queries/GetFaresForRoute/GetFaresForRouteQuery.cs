using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.Common.Configuration;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Routes.Queries.GetFaresForRoute;

public record GetFaresForRouteQuery(Guid RouteId) : IRequest<List<FareMatrixDto>>;

public class GetFaresForRouteQueryHandler(
    IRouteService _routeService,
    HybridCache _cache,
    IOptions<CacheSettings> _cacheSettings)
    : IRequestHandler<GetFaresForRouteQuery, List<FareMatrixDto>>
{
    public async Task<List<FareMatrixDto>> Handle(GetFaresForRouteQuery request, CancellationToken cancellationToken)
    {
        var entryOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(_cacheSettings.Value.FareMatrixExpirationMinutes),
            LocalCacheExpiration = TimeSpan.FromMinutes(_cacheSettings.Value.FareMatrixExpirationMinutes)
        };

        return await _cache.GetOrCreateAsync(
            CacheKeys.FaresForRoute(request.RouteId),
            async ct => await _routeService.GetFaresForRouteAsync(request.RouteId, ct),
            entryOptions,
            tags: [CacheKeys.TagFares],
            cancellationToken: cancellationToken);
    }
}

