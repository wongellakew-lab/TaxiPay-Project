using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Passengers;
using TaxiPayApi.Application.Services;

namespace TaxiPayApi.Application.Features.Passengers.Commands.PayTrip;

public class PayTripCommandHandler(IPassengerService _passengerService)
    : IRequestHandler<PayTripCommand, Result<PayTripResponse, PassengerError>>
{
    public async Task<Result<PayTripResponse, PassengerError>> Handle(
        PayTripCommand request,
        CancellationToken cancellationToken)
    {
        return await _passengerService.PayForTripAsync(
            request.PassengerId,
            request.Request,
            cancellationToken);
    }
}