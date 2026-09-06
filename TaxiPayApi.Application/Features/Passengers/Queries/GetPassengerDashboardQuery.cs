using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Passengers;

namespace TaxiPayApi.Application.Features.Passengers.Queries.GetDashboard;

public record GetPassengerDashboardQuery(Guid PassengerId)
    : IRequest<Result<PassengerDashboardResponse, PassengerError>>;