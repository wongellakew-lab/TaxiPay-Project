using MediatR;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Admins.Queries.GetAdminAnalyticsOverview;

public record GetAdminAnalyticsOverviewQuery : IRequest<AdminAnalyticsOverviewDto>;

public class GetAdminAnalyticsOverviewQueryHandler(IAdminService _adminService)
    : IRequestHandler<GetAdminAnalyticsOverviewQuery, AdminAnalyticsOverviewDto>
{
    public async Task<AdminAnalyticsOverviewDto> Handle(GetAdminAnalyticsOverviewQuery request, CancellationToken cancellationToken)
    {
        return await _adminService.GetAnalyticsOverviewAsync(cancellationToken);
    }
}
