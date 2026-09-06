using FluentValidation;

namespace TaxiPayApi.Application.Features.Admins.Queries.GetAdminRouteById;

public class GetAdminRouteByIdQueryValidator : AbstractValidator<GetAdminRouteByIdQuery>
{
    public GetAdminRouteByIdQueryValidator()
    {
        RuleFor(x => x.RouteId)
            .NotEmpty().WithMessage("Route ID is required.");
    }
}
