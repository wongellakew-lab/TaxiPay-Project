using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Drivers.Commands.CreateDriver;

public class CreateDriverCommandHandler(IDriverService _driverService)
    : IRequestHandler<CreateDriverCommand, Result<DriverResponse, DriverError>>
{
    public async Task<Result<DriverResponse, DriverError>> Handle(
    CreateDriverCommand request,
    CancellationToken cancellationToken)
    {
        return await _driverService.CreateDriverAsync(request.Request, cancellationToken);
    }
}