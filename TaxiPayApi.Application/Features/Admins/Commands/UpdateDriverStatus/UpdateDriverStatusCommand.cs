using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Admins.Commands.UpdateDriverStatus;

public record UpdateDriverStatusCommand(Guid DriverId, UpdateDriverStatusRequest Request) : IRequest<bool>;

public class UpdateDriverStatusCommandHandler(IAdminService _adminService, HybridCache _cache)
    : IRequestHandler<UpdateDriverStatusCommand, bool>
{
    public async Task<bool> Handle(UpdateDriverStatusCommand request, CancellationToken cancellationToken)
    {
        var success = await _adminService.UpdateDriverStatusAsync(request.DriverId, request.Request, cancellationToken);
        if (success)
        {
            await _cache.RemoveByTagAsync(CacheKeys.TagDrivers, cancellationToken);
        }
        return success;
    }
}

