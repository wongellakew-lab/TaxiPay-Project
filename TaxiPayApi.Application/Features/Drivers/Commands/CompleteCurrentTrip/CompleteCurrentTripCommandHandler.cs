using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Drivers.Commands.CompleteCurrentTrip;

public class CompleteCurrentTripCommandHandler(
    IDriverService _driverService,
    HybridCache _cache)
    : IRequestHandler<CompleteCurrentTripCommand, Result<TripResponse, DriverError>>
{
    public async Task<Result<TripResponse, DriverError>> Handle(
        CompleteCurrentTripCommand request,
        CancellationToken cancellationToken)
    {
        var result = await _driverService.CompleteCurrentTripAsync(
            request.ShiftId,
            request.DriverLatitude,
            request.DriverLongitude,
            cancellationToken);

        if (result.IsSuccess)
        {
            await _cache.RemoveByTagAsync(CacheKeys.TagDrivers, cancellationToken);
        }

        return result;
    }
}