using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Passengers;

namespace TaxiPayApi.Application.Features.Passengers.Commands.Login;

public record LoginPassengerCommand(LoginRequest Request)
    : IRequest<Result<PassengerLoginResponse, PassengerError>>;