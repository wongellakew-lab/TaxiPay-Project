using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.Common.Configuration;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Admins.Queries.GetAdminRoutes;

public record GetAdminRoutesQuery : IRequest<List<RouteDetailDto>>;

public class GetAdminRoutesQueryHandler(
    IAdminService _adminService,
    HybridCache _cache,
    IOptions<CacheSettings> _cacheSettings)
    : IRequestHandler<GetAdminRoutesQuery, List<RouteDetailDto>>
{
    public async Task<List<RouteDetailDto>> Handle(GetAdminRoutesQuery request, CancellationToken cancellationToken)
    {
        var entryOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(_cacheSettings.Value.RouteExpirationMinutes),
            LocalCacheExpiration = TimeSpan.FromMinutes(_cacheSettings.Value.RouteExpirationMinutes)
        };

        return await _cache.GetOrCreateAsync(
            CacheKeys.RoutesAll,
            async ct => await _adminService.GetRoutesAsync(ct),
            entryOptions,
            tags: [CacheKeys.TagRoutes],
            cancellationToken: cancellationToken);
    }
}

