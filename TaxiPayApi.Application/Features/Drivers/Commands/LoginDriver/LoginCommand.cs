using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Drivers;

namespace TaxiPayApi.Application.Features.Drivers.Commands;

public record LoginCommand(DriverLoginRequest Request)
    : IRequest<Result<LoginResponse, DriverError>>;