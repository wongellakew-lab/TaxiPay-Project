using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Passengers;

namespace TaxiPayApi.Application.Features.Passengers.Queries.GetTripHistory;

public record GetPassengerTripHistoryQuery(
    Guid PassengerId,
    int PageNumber = 1,
    int PageSize = 10)
    : IRequest<Result<TripHistoryResponse, PassengerError>>;