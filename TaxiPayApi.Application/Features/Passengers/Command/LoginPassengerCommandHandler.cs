using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Passengers;
using TaxiPayApi.Application.Services;

namespace TaxiPayApi.Application.Features.Passengers.Commands.Login;

public class LoginPassengerCommandHandler(IPassengerService _passengerService)
    : IRequestHandler<LoginPassengerCommand, Result<PassengerLoginResponse, PassengerError>>
{
    public async Task<Result<PassengerLoginResponse, PassengerError>> Handle(
        LoginPassengerCommand request,
        CancellationToken cancellationToken)
    {
        return await _passengerService.LoginAsync(request.Request, cancellationToken);
    }
}