using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Passengers;
using TaxiPayApi.Application.Services;

namespace TaxiPayApi.Application.Features.Passengers.Queries.GetPassengerRouteRequests;

public record GetPassengerRouteRequestsQuery(Guid PassengerId) : IRequest<Result<List<PassengerRouteRequestDto>, PassengerError>>;

public class GetPassengerRouteRequestsQueryHandler(IPassengerService _passengerService)
    : IRequestHandler<GetPassengerRouteRequestsQuery, Result<List<PassengerRouteRequestDto>, PassengerError>>
{
    public async Task<Result<List<PassengerRouteRequestDto>, PassengerError>> Handle(
        GetPassengerRouteRequestsQuery request,
        CancellationToken cancellationToken)
    {
        return await _passengerService.GetPassengerRouteRequestsAsync(
            request.PassengerId,
            cancellationToken);
    }
}
