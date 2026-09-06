using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Drivers;

namespace TaxiPayApi.Application.Features.Drivers.Commands.UpdateProfile;

public record UpdateDriverProfileCommand(Guid DriverId, UpdateProfileRequest Request)
    : IRequest<Result<DriverResponse, DriverError>>;