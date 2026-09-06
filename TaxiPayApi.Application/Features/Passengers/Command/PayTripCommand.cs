using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Passengers;

namespace TaxiPayApi.Application.Features.Passengers.Commands.PayTrip;

public record PayTripCommand(
    Guid PassengerId,
    PayTripRequest Request)
    : IRequest<Result<PayTripResponse, PassengerError>>;