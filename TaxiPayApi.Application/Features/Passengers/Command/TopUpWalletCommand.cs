using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Passengers;

namespace TaxiPayApi.Application.Features.Passengers.Commands.TopUpWallet;

public record TopUpWalletCommand(
    Guid PassengerId,
    TopUpWalletRequest Request)
    : IRequest<Result<TopUpWalletResponse, PassengerError>>;
