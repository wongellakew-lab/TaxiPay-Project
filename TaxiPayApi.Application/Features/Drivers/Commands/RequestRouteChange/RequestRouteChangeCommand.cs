using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Drivers;

namespace TaxiPayApi.Application.Features.Drivers.Commands.RequestRouteChange;

public record RequestRouteChangeCommand(
    Guid DriverId,
    RequestRouteChangeRequest Request)
    : IRequest<Result<RouteChangeResponse, DriverError>>;