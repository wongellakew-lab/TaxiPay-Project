using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Passengers;

namespace TaxiPayApi.Application.Features.Passengers.Queries.GetDriverByCode;

public record GetDriverByCodeQuery(string DriverId)
    : IRequest<Result<DriverByCodeResponse, PassengerError>>;