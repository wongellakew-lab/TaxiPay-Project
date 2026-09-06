using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Passengers;
using TaxiPayApi.Application.Services;

namespace TaxiPayApi.Application.Features.Passengers.Queries.GetTripHistory;

public class GetPassengerTripHistoryQueryHandler(IPassengerService _passengerService)
    : IRequestHandler<GetPassengerTripHistoryQuery, Result<TripHistoryResponse, PassengerError>>
{
    public async Task<Result<TripHistoryResponse, PassengerError>> Handle(
        GetPassengerTripHistoryQuery request,
        CancellationToken cancellationToken)
    {
        return await _passengerService.GetTripHistoryAsync(
            request.PassengerId,
            request.PageNumber,
            request.PageSize,
            cancellationToken);
    }
}