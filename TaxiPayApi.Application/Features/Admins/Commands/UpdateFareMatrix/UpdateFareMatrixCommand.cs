using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Admins.Commands.UpdateFareMatrix;

public record UpdateFareMatrixCommand(Guid Id, UpdateFareMatrixRequest Request) : IRequest<FareMatrixDto?>;

public class UpdateFareMatrixCommandHandler(IAdminService _adminService, HybridCache _cache)
    : IRequestHandler<UpdateFareMatrixCommand, FareMatrixDto?>
{
    public async Task<FareMatrixDto?> Handle(UpdateFareMatrixCommand request, CancellationToken cancellationToken)
    {
        var result = await _adminService.UpdateFareMatrixAsync(request.Id, request.Request, cancellationToken);
        if (result != null)
        {
            await _cache.RemoveByTagAsync(CacheKeys.TagFares, cancellationToken);
            await _cache.RemoveAsync(CacheKeys.FaresForRoute(result.RouteId), cancellationToken);
        }
        return result;
    }
}

