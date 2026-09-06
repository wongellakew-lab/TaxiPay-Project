using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Admins.Commands.ApproveDriver;

public record ApproveDriverCommand(Guid DriverId, ApproveDriverRequest Request) : IRequest<bool>;

public class ApproveDriverCommandHandler(IAdminService _adminService, HybridCache _cache)
    : IRequestHandler<ApproveDriverCommand, bool>
{
    public async Task<bool> Handle(ApproveDriverCommand request, CancellationToken cancellationToken)
    {
        var success = await _adminService.ApproveDriverAsync(request.DriverId, request.Request, cancellationToken);
        if (success)
        {
            await _cache.RemoveByTagAsync(CacheKeys.TagDrivers, cancellationToken);
        }
        return success;
    }
}

