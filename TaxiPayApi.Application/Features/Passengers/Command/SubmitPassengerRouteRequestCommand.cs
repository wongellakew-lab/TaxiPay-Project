using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Passengers;
using TaxiPayApi.Application.Services;

namespace TaxiPayApi.Application.Features.Passengers.Commands.SubmitPassengerRouteRequest;

public record SubmitPassengerRouteRequestCommand(
    Guid PassengerId,
    CreatePassengerRouteRequestDto Request) : IRequest<Result<PassengerRouteRequestDto, PassengerError>>;

public class SubmitPassengerRouteRequestCommandHandler(IPassengerService _passengerService)
    : IRequestHandler<SubmitPassengerRouteRequestCommand, Result<PassengerRouteRequestDto, PassengerError>>
{
    public async Task<Result<PassengerRouteRequestDto, PassengerError>> Handle(
        SubmitPassengerRouteRequestCommand request,
        CancellationToken cancellationToken)
    {
        return await _passengerService.SubmitRouteRequestAsync(
            request.PassengerId,
            request.Request,
            cancellationToken);
    }
}
