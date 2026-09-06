using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Drivers.Commands.UpdateProfile;

public class UpdateDriverProfileCommandHandler(IDriverService _driverService, HybridCache _cache)
    : IRequestHandler<UpdateDriverProfileCommand, Result<DriverResponse, DriverError>>
{
    public async Task<Result<DriverResponse, DriverError>> Handle(
         UpdateDriverProfileCommand request,
         CancellationToken cancellationToken)
    {
        var result = await _driverService.UpdateProfileAsync(request.DriverId, request.Request, cancellationToken);
        if (result.IsSuccess)
        {
            await _cache.RemoveByTagAsync(CacheKeys.TagDrivers, cancellationToken);
        }
        return result;
    }
}