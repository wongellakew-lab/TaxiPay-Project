using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.Common.Configuration;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Admins.Queries.GetFareMatricesByRoute;

public record GetFareMatricesByRouteQuery(Guid RouteId) : IRequest<List<FareMatrixDto>>;

public class GetFareMatricesByRouteQueryHandler(
    IAdminService _adminService,
    HybridCache _cache,
    IOptions<CacheSettings> _cacheSettings)
    : IRequestHandler<GetFareMatricesByRouteQuery, List<FareMatrixDto>>
{
    public async Task<List<FareMatrixDto>> Handle(GetFareMatricesByRouteQuery request, CancellationToken cancellationToken)
    {
        var entryOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(_cacheSettings.Value.FareMatrixExpirationMinutes),
            LocalCacheExpiration = TimeSpan.FromMinutes(_cacheSettings.Value.FareMatrixExpirationMinutes)
        };

        return await _cache.GetOrCreateAsync(
            CacheKeys.FaresForRoute(request.RouteId),
            async ct => await _adminService.GetFareMatricesByRouteAsync(request.RouteId, ct),
            entryOptions,
            tags: [CacheKeys.TagFares],
            cancellationToken: cancellationToken);
    }
}

