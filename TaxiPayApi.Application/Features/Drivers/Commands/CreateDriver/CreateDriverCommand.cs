using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Drivers;

namespace TaxiPayApi.Application.Features.Drivers.Commands.CreateDriver;

public record CreateDriverCommand(CreateDriverRequest Request) : IRequest<Result<DriverResponse, DriverError>>;