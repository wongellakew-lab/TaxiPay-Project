using MediatR;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Passengers;
using TaxiPayApi.Application.Services;

namespace TaxiPayApi.Application.Features.Passengers.Queries.SearchDriversByCode;

public record SearchDriversByCodeQuery(string Query, int Limit = 10) : IRequest<Result<List<DriverSearchResultDto>, PassengerError>>;

public class SearchDriversByCodeQueryHandler(IPassengerService _passengerService)
    : IRequestHandler<SearchDriversByCodeQuery, Result<List<DriverSearchResultDto>, PassengerError>>
{
    public async Task<Result<List<DriverSearchResultDto>, PassengerError>> Handle(
        SearchDriversByCodeQuery request,
        CancellationToken cancellationToken)
    {
        return await _passengerService.SearchDriversByCodeAsync(
            request.Query,
            request.Limit,
            cancellationToken);
    }
}
