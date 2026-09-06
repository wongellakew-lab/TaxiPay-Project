using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Application.DTOs.Passengers;
using TaxiPayApi.Application.Services;

namespace TaxiPayApi.Application.Features.Passengers.Commands.ConfirmAlighting;

public class ConfirmAlightingCommandHandler(IPassengerService _passengerService)
    : IRequestHandler<ConfirmAlightingCommand, Result<ActivePassengerDto, PassengerError>>
{
    public async Task<Result<ActivePassengerDto, PassengerError>> Handle(
        ConfirmAlightingCommand request,
        CancellationToken cancellationToken)
    {
        return await _passengerService.ConfirmPassengerAlightingAsync(
            request.PassengerId,
            request.JourneyId,
            cancellationToken);
    }
}
