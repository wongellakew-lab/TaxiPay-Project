using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Drivers;

namespace TaxiPayApi.Application.Features.Drivers.Queries.GetDashboard;

public record GetDriverDashboardQuery(Guid DriverId)
    : IRequest<Result<DriverDashboardResponse, DriverError>>;