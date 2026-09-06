using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Application.Interfaces;


namespace TaxiPayApi.Application.Features.Drivers.Queries.GetShiftSummary;

public class GetShiftSummaryQueryHandler(IDriverService _driverService)
    : IRequestHandler<GetShiftSummaryQuery, Result<ShiftSummaryResponse, DriverError>>
{
    public async Task<Result<ShiftSummaryResponse, DriverError>> Handle(
        GetShiftSummaryQuery request,
        CancellationToken cancellationToken)
    {
        return await _driverService.GetShiftSummaryAsync(request.ShiftId, cancellationToken);
    }
}