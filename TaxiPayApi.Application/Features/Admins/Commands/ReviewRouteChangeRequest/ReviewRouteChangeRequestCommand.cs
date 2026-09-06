using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Admins.Commands.ReviewRouteChangeRequest;

public record ReviewRouteChangeRequestCommand(
    Guid RequestId,
    Guid AdminId,
    ReviewRouteChangeRequestDto Request) : IRequest<bool>;

public class ReviewRouteChangeRequestCommandHandler(IAdminService _adminService, HybridCache _cache)
    : IRequestHandler<ReviewRouteChangeRequestCommand, bool>
{
    public async Task<bool> Handle(ReviewRouteChangeRequestCommand request, CancellationToken cancellationToken)
    {
        var success = await _adminService.ReviewRouteChangeRequestAsync(
            request.RequestId,
            request.AdminId,
            request.Request,
            cancellationToken);

        if (success)
        {
            await _cache.RemoveByTagAsync(CacheKeys.TagDrivers, cancellationToken);
            await _cache.RemoveByTagAsync(CacheKeys.TagRoutes, cancellationToken);
        }

        return success;
    }
}
