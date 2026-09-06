using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Drivers.Commands.StartNewTrip;

public class StartNewTripCommandHandler(
    IDriverService _driverService,
    HybridCache _cache)
    : IRequestHandler<StartNewTripCommand, Result<TripResponse, DriverError>>
{
    public async Task<Result<TripResponse, DriverError>> Handle(
        StartNewTripCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _driverService.StartNewTripAsync(
            request.ShiftId,
            request.Direction,
            cancellationToken);

        if (result.IsSuccess)
        {
            await _cache.RemoveByTagAsync(CacheKeys.TagDrivers, cancellationToken);
        }

        return result;
    }
}