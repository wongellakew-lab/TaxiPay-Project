using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Application.Features.Drivers.Queries.GetProfile;

public class GetDriverProfileQueryHandler(IDriverService _driverService)
    : IRequestHandler<GetDriverProfileQuery, Result<DriverResponse, DriverError>>
{
    public async Task<Result<DriverResponse, DriverError>> Handle(
        GetDriverProfileQuery request,
        CancellationToken cancellationToken)
    {
        return await _driverService.GetProfileAsync(request.DriverId, cancellationToken);
    }
}