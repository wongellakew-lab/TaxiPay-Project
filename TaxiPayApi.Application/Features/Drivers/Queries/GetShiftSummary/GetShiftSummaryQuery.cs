using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Drivers;

namespace TaxiPayApi.Application.Features.Drivers.Queries.GetShiftSummary;

public record GetShiftSummaryQuery(Guid ShiftId)
    : IRequest<Result<ShiftSummaryResponse, DriverError>>;