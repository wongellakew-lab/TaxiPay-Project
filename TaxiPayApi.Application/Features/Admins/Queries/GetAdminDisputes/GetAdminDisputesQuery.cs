using MediatR;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Admins.Queries.GetAdminDisputes;

public record GetAdminDisputesQuery(string? Status = null) : IRequest<List<AdminDisputeDto>>;

public class GetAdminDisputesQueryHandler(IAdminService _adminService)
    : IRequestHandler<GetAdminDisputesQuery, List<AdminDisputeDto>>
{
    public async Task<List<AdminDisputeDto>> Handle(GetAdminDisputesQuery request, CancellationToken cancellationToken)
    {
        return await _adminService.GetDisputesAsync(request.Status, cancellationToken);
    }
}
