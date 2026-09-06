using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Drivers;

namespace TaxiPayApi.Application.Features.Drivers.Commands.StartNewTrip;

public record StartNewTripCommand(
    Guid ShiftId,
    string Direction)
    : IRequest<Result<TripResponse, DriverError>>;