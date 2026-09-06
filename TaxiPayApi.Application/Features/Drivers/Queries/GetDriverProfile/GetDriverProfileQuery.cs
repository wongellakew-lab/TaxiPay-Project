using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Drivers;

namespace TaxiPayApi.Application.Features.Drivers.Queries.GetProfile;

public record GetDriverProfileQuery(Guid DriverId)
    : IRequest<Result<DriverResponse, DriverError>>;