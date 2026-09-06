using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Drivers.Commands.Login;

public class LoginCommandHandler(IDriverService _driverService)
    : IRequestHandler<LoginCommand, Result<LoginResponse, DriverError>>
{
    public async Task<Result<LoginResponse, DriverError>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        return await _driverService.LoginAsync(request.Request, cancellationToken);
    }
}