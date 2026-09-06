using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Passengers;

namespace TaxiPayApi.Application.Features.Passengers.Queries.GetFare;

public record GetFareQuery(
    Guid RouteId,
    string FromNode,
    string ToNode)
    : IRequest<Result<FareResponse, PassengerError>>;