using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Admins.Commands.RejectDriver;

public record RejectDriverCommand(Guid DriverId, RejectDriverRequest Request) : IRequest<bool>;

public class RejectDriverCommandHandler(IAdminService _adminService, HybridCache _cache)
    : IRequestHandler<RejectDriverCommand, bool>
{
    public async Task<bool> Handle(RejectDriverCommand request, CancellationToken cancellationToken)
    {
        var success = await _adminService.RejectDriverAsync(request.DriverId, request.Request, cancellationToken);
        if (success)
        {
            await _cache.RemoveByTagAsync(CacheKeys.TagDrivers, cancellationToken);
        }
        return success;
    }
}
