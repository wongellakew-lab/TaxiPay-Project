using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Admins.Commands.AssignDriverRoute;

public record AssignDriverRouteCommand(Guid DriverId, AssignDriverRouteRequest Request) : IRequest<bool>;

public class AssignDriverRouteCommandHandler(IAdminService _adminService, HybridCache _cache)
    : IRequestHandler<AssignDriverRouteCommand, bool>
{
    public async Task<bool> Handle(AssignDriverRouteCommand request, CancellationToken cancellationToken)
    {
        var success = await _adminService.AssignDriverRouteAsync(request.DriverId, request.Request, cancellationToken);
        if (success)
        {
            await _cache.RemoveByTagAsync(CacheKeys.TagDrivers, cancellationToken);
            await _cache.RemoveByTagAsync(CacheKeys.TagRoutes, cancellationToken);
        }
        return success;
    }
}
