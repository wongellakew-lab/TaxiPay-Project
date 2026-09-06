using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Application.DTOs.Passengers;

namespace TaxiPayApi.Application.Features.Passengers.Commands.ConfirmAlighting;

public record ConfirmAlightingCommand(Guid PassengerId, Guid JourneyId)
    : IRequest<Result<ActivePassengerDto, PassengerError>>;
