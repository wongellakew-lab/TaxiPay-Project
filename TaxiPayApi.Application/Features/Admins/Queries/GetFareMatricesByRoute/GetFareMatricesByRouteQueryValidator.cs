using FluentValidation;

namespace TaxiPayApi.Application.Features.Admins.Queries.GetFareMatricesByRoute;

public class GetFareMatricesByRouteQueryValidator : AbstractValidator<GetFareMatricesByRouteQuery>
{
    public GetFareMatricesByRouteQueryValidator()
    {
        RuleFor(x => x.RouteId)
            .NotEmpty().WithMessage("Route ID is required.");
    }
}
