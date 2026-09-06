using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Drivers.Commands.RequestAlightingConfirmation;

public class RequestAlightingConfirmationCommandHandler(IDriverService _driverService)
    : IRequestHandler<RequestAlightingConfirmationCommand, Result<ActivePassengerDto, DriverError>>
{
    public async Task<Result<ActivePassengerDto, DriverError>> Handle(
        RequestAlightingConfirmationCommand request,
        CancellationToken cancellationToken)
    {
        return await _driverService.RequestPassengerAlightingAsync(
            request.JourneyId,
            cancellationToken);
    }
}
