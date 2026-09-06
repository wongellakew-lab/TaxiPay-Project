using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Passengers;

namespace TaxiPayApi.Application.Features.Passengers.Commands.UpdateProfile;

public record UpdatePassengerProfileCommand(
    Guid PassengerId,
    UpdatePassengerProfileRequest Request)
    : IRequest<Result<PassengerResponse, PassengerError>>;
