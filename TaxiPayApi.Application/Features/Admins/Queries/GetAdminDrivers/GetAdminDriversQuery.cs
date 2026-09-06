using MediatR;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Admins.Queries.GetAdminDrivers;

public record GetAdminDriversQuery(string? Status = null) : IRequest<List<AdminDriverListItemDto>>;

public class GetAdminDriversQueryHandler(IAdminService _adminService)
    : IRequestHandler<GetAdminDriversQuery, List<AdminDriverListItemDto>>
{
    public async Task<List<AdminDriverListItemDto>> Handle(GetAdminDriversQuery request, CancellationToken cancellationToken)
    {
        return await _adminService.GetDriversAsync(request.Status, cancellationToken);
    }
}
