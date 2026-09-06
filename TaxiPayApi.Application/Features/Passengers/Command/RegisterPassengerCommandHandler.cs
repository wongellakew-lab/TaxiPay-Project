using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Passengers;
using TaxiPayApi.Application.Services;

namespace TaxiPayApi.Application.Features.Passengers.Commands.Register;

public class RegisterPassengerCommandHandler
    : IRequestHandler<RegisterPassengerCommand, Result<PassengerResponse, PassengerError>>
{
    private readonly IPassengerService _passengerService;

    public RegisterPassengerCommandHandler(IPassengerService passengerService)
    {
        _passengerService = passengerService;
    }

    public async Task<Result<PassengerResponse, PassengerError>> Handle(
        RegisterPassengerCommand request,
        CancellationToken cancellationToken)
    {
        return await _passengerService.RegisterAsync(request.Request, cancellationToken);
    }
}