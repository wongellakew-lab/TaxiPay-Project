using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.Common.Configuration;
using TaxiPayApi.Application.DTOs.Passengers;
using TaxiPayApi.Application.Services;

namespace TaxiPayApi.Application.Features.Passengers.Queries.GetDriverByCode;

public class GetDriverByCodeQueryHandler(
    IPassengerService _passengerService,
    HybridCache _cache,
    IOptions<CacheSettings> _cacheSettings)
    : IRequestHandler<GetDriverByCodeQuery, Result<DriverByCodeResponse, PassengerError>>
{
    public async Task<Result<DriverByCodeResponse, PassengerError>> Handle(
        GetDriverByCodeQuery request,
        CancellationToken cancellationToken)
    {
        var cacheKey = CacheKeys.DriverByCode(request.DriverId);
        var entryOptions = new HybridCacheEntryOptions
        {
            Expiration = TimeSpan.FromMinutes(_cacheSettings.Value.DriverLookupExpirationMinutes),
            LocalCacheExpiration = TimeSpan.FromMinutes(_cacheSettings.Value.DriverLookupExpirationMinutes)
        };

        return await _cache.GetOrCreateAsync(
            cacheKey,
            async ct => await _passengerService.GetDriverByCodeAsync(request.DriverId, ct),
            entryOptions,
            tags: [CacheKeys.TagDrivers],
            cancellationToken: cancellationToken);
    }
}