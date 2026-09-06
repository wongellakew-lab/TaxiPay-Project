using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Passengers;
using TaxiPayApi.Application.Services;

namespace TaxiPayApi.Application.Features.Passengers.Commands.TopUpWallet;

public class TopUpWalletCommandHandler(IPassengerService _passengerService)
    : IRequestHandler<TopUpWalletCommand, Result<TopUpWalletResponse, PassengerError>>
{
    public async Task<Result<TopUpWalletResponse, PassengerError>> Handle(
        TopUpWalletCommand request,
        CancellationToken cancellationToken)
    {
        return await _passengerService.TopUpWalletAsync(
            request.PassengerId,
            request.Request,
            cancellationToken);
    }
}
