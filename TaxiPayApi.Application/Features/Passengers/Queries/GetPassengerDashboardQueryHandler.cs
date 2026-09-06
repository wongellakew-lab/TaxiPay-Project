using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Passengers;
using TaxiPayApi.Application.Services;

namespace TaxiPayApi.Application.Features.Passengers.Queries.GetDashboard;

public class GetPassengerDashboardQueryHandler(IPassengerService _passengerService)
    : IRequestHandler<GetPassengerDashboardQuery, Result<PassengerDashboardResponse, PassengerError>>
{
    public async Task<Result<PassengerDashboardResponse, PassengerError>> Handle(
        GetPassengerDashboardQuery request,
        CancellationToken cancellationToken)
    {
        return await _passengerService.GetDashboardAsync(request.PassengerId, cancellationToken);
    }
}