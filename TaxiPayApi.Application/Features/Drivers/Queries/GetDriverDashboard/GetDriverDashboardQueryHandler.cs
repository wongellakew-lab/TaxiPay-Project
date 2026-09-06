using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Drivers.Queries.GetDashboard;

public class GetDriverDashboardQueryHandler(IDriverService _driverService)
    : IRequestHandler<GetDriverDashboardQuery, Result<DriverDashboardResponse, DriverError>>
{
    public async Task<Result<DriverDashboardResponse, DriverError>> Handle(
        GetDriverDashboardQuery request,
        CancellationToken cancellationToken)
    {
        return await _driverService.GetDashboardAsync(request.DriverId, cancellationToken);
    }
}