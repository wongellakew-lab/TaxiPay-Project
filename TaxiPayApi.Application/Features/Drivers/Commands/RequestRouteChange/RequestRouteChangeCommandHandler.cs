using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Drivers.Commands.RequestRouteChange;

public class RequestRouteChangeCommandHandler(IDriverService _driverService)
    : IRequestHandler<RequestRouteChangeCommand, Result<RouteChangeResponse, DriverError>>
{
    public async Task<Result<RouteChangeResponse, DriverError>> Handle(
        RequestRouteChangeCommand request,
        CancellationToken cancellationToken)
    {
        return await _driverService.RequestRouteChangeAsync(
            request.DriverId,
            request.Request,
            cancellationToken);
    }
}