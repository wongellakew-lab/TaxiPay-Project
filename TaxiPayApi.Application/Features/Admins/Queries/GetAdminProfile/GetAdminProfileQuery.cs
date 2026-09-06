using MediatR;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Admins.Queries.GetAdminProfile;

public record GetAdminProfileQuery(Guid AdminId) : IRequest<AdminProfileResponse?>;

public class GetAdminProfileQueryHandler(IAdminService _adminService)
    : IRequestHandler<GetAdminProfileQuery, AdminProfileResponse?>
{
    public async Task<AdminProfileResponse?> Handle(GetAdminProfileQuery request, CancellationToken cancellationToken)
    {
        return await _adminService.GetProfileAsync(request.AdminId, cancellationToken);
    }
}
