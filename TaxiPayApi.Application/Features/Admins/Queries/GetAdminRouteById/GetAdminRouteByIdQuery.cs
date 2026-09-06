using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.Common.Configuration;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Admins.Queries.GetAdminRouteById;

public record GetAdminRouteByIdQuery(Guid RouteId) : IRequest<RouteDetailDto?>;

public class GetAdminRouteByIdQueryHandler(
    IAdminService _adminService,
    HybridCache _cache,
    IOptions<CacheSettings> _cacheSettings)
    : IRequestHandler<GetAdminRouteByIdQuery, RouteDetailDto?>
{
    public async Task<RouteDetailDto?> Handle(GetAdminRouteByIdQuery request, CancellationToken cancellationToken)
    {
        var entryOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(_cacheSettings.Value.RouteExpirationMinutes),
            LocalCacheExpiration = TimeSpan.FromMinutes(_cacheSettings.Value.RouteExpirationMinutes)
        };

        return await _cache.GetOrCreateAsync(
            CacheKeys.RouteById(request.RouteId),
            async ct => await _adminService.GetRouteByIdAsync(request.RouteId, ct),
            entryOptions,
            tags: [CacheKeys.TagRoutes],
            cancellationToken: cancellationToken);
    }
}

