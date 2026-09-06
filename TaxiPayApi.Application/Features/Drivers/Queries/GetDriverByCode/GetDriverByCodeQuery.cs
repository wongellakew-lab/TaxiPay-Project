using MediatR;
using TaxiPayApi.Application.DTOs.Drivers;

namespace TaxiPayApi.Application.Features.Drivers.Queries;

public record GetDriverByCodeQuery(string DriverId) : IRequest<DriverResponse>;