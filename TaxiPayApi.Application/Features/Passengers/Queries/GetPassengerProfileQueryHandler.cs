using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Passengers;
using TaxiPayApi.Application.Services;

namespace TaxiPayApi.Application.Features.Passengers.Queries.GetProfile;

public class GetPassengerProfileQueryHandler(IPassengerService _passengerService)
    : IRequestHandler<GetPassengerProfileQuery, Result<PassengerResponse, PassengerError>>
{
    public async Task<Result<PassengerResponse, PassengerError>> Handle(
        GetPassengerProfileQuery request,
        CancellationToken cancellationToken)
    {
        return await _passengerService.GetProfileAsync(request.PassengerId, cancellationToken);
    }
}