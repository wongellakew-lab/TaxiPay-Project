using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Drivers;

namespace TaxiPayApi.Application.Features.Drivers.Commands.RequestAlightingConfirmation;

public record RequestAlightingConfirmationCommand(Guid JourneyId)
    : IRequest<Result<ActivePassengerDto, DriverError>>;
