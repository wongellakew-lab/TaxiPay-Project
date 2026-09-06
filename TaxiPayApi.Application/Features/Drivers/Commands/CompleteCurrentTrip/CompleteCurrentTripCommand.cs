using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Drivers;

namespace TaxiPayApi.Application.Features.Drivers.Commands.CompleteCurrentTrip;

public record CompleteCurrentTripCommand(
    Guid ShiftId,
    decimal DriverLatitude,
    decimal DriverLongitude)
    : IRequest<Result<TripResponse, DriverError>>;