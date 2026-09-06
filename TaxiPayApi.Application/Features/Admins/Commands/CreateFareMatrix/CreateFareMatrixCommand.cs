using MediatR;
using Microsoft.Extensions.Caching.Hybrid;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Admins.Commands.CreateFareMatrix;

public record CreateFareMatrixCommand(CreateFareMatrixRequest Request) : IRequest<FareMatrixDto>;

public class CreateFareMatrixCommandHandler(IAdminService _adminService, HybridCache _cache)
    : IRequestHandler<CreateFareMatrixCommand, FareMatrixDto>
{
    public async Task<FareMatrixDto> Handle(CreateFareMatrixCommand request, CancellationToken cancellationToken)
    {
        var result = await _adminService.CreateFareMatrixAsync(request.Request, cancellationToken);
        if (result != null)
        {
            await _cache.RemoveByTagAsync(CacheKeys.TagFares, cancellationToken);
            await _cache.RemoveAsync(CacheKeys.FaresForRoute(result.RouteId), cancellationToken);
        }
        return result;
    }
}

