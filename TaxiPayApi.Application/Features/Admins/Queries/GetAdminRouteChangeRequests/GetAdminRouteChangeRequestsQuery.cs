using MediatR;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Admins.Queries.GetAdminRouteChangeRequests;

public record GetAdminRouteChangeRequestsQuery(string? Status = null) : IRequest<List<AdminRouteChangeRequestDto>>;

public class GetAdminRouteChangeRequestsQueryHandler(IAdminService _adminService)
    : IRequestHandler<GetAdminRouteChangeRequestsQuery, List<AdminRouteChangeRequestDto>>
{
    public async Task<List<AdminRouteChangeRequestDto>> Handle(GetAdminRouteChangeRequestsQuery request, CancellationToken cancellationToken)
    {
        return await _adminService.GetRouteChangeRequestsAsync(request.Status, cancellationToken);
    }
}
