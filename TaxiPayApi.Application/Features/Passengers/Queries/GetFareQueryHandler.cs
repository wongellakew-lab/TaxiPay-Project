using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.Common.Configuration;
using TaxiPayApi.Application.DTOs.Passengers;
using TaxiPayApi.Application.Services;

namespace TaxiPayApi.Application.Features.Passengers.Queries.GetFare;

public class GetFareQueryHandler(
    IPassengerService _passengerService,
    HybridCache _cache,
    IOptions<CacheSettings> _cacheSettings)
    : IRequestHandler<GetFareQuery, Result<FareResponse, PassengerError>>
{
    public async Task<Result<FareResponse, PassengerError>> Handle(
        GetFareQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.Fare(request.RouteId, request.FromNode, request.ToNode);
        var entryOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(_cacheSettings.Value.FareMatrixExpirationMinutes),
            LocalCacheExpiration = TimeSpan.FromMinutes(_cacheSettings.Value.FareMatrixExpirationMinutes)
        };

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async ct => await _passengerService.GetFareAsync(
                request.RouteId,
                request.FromNode,
                request.ToNode,
                ct),
            entryOptions,
            tags: [CacheKeys.TagFares],
            cancellationToken: cancellationToken);
    }
}