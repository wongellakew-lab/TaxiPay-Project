using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Admins.Commands.GenerateSymmetricFares;

public record GenerateSymmetricFaresCommand(GenerateSymmetricFareRequest Request) : IRequest<List<FareMatrixDto>>;

public class GenerateSymmetricFaresCommandHandler(IAdminService _adminService, HybridCache _cache)
    : IRequestHandler<GenerateSymmetricFaresCommand, List<FareMatrixDto>>
{
    public async Task<List<FareMatrixDto>> Handle(GenerateSymmetricFaresCommand request, CancellationToken cancellationToken)
    {
        var result = await _adminService.GenerateSymmetricFaresAsync(request.Request, cancellationToken);
        if (result != null)
        {
            await _cache.RemoveByTagAsync(CacheKeys.TagFares, cancellationToken);
            await _cache.RemoveAsync(CacheKeys.FaresForRoute(request.Request.RouteId), cancellationToken);
        }
        return result;
    }
}

