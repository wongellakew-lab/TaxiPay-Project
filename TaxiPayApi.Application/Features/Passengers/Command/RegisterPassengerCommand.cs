using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Passengers;

namespace TaxiPayApi.Application.Features.Passengers.Commands.Register;

public record RegisterPassengerCommand(RegisterRequest Request)
    : IRequest<Result<PassengerResponse, PassengerError>>;