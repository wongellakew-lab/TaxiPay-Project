using MediatR;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Admins.Queries.GetAdminDriverById;

public record GetAdminDriverByIdQuery(Guid DriverId) : IRequest<AdminDriverListItemDto?>;

public class GetAdminDriverByIdQueryHandler(IAdminService _adminService)
    : IRequestHandler<GetAdminDriverByIdQuery, AdminDriverListItemDto?>
{
    public async Task<AdminDriverListItemDto?> Handle(GetAdminDriverByIdQuery request, CancellationToken cancellationToken)
    {
        return await _adminService.GetDriverByIdAsync(request.DriverId, cancellationToken);
    }
}
