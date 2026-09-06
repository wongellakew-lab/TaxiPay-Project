using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Passengers;

namespace TaxiPayApi.Application.Features.Passengers.Queries.GetProfile;

public record GetPassengerProfileQuery(Guid PassengerId)
    : IRequest<Result<PassengerResponse, PassengerError>>;