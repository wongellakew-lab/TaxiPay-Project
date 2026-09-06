using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Passengers;
using TaxiPayApi.Application.Services;

namespace TaxiPayApi.Application.Features.Passengers.Commands.UpdateProfile;

public class UpdatePassengerProfileCommandHandler(IPassengerService _passengerService)
    : IRequestHandler<UpdatePassengerProfileCommand, Result<PassengerResponse, PassengerError>>
{
    public async Task<Result<PassengerResponse, PassengerError>> Handle(
        UpdatePassengerProfileCommand request,
        CancellationToken cancellationToken)
    {
        return await _passengerService.UpdateProfileAsync(
            request.PassengerId,
            request.Request,
            cancellationToken);
    }
}
