using FluentValidation;

namespace TaxiPayApi.Application.Features.Admins.Commands.AssignDriverRoute;

public class AssignDriverRouteCommandValidator : AbstractValidator<AssignDriverRouteCommand>
{
    public AssignDriverRouteCommandValidator()
    {
        RuleFor(x => x.DriverId)
            .NotEmpty().WithMessage("Driver ID is required.");

        RuleFor(x => x.Request.RouteId)
            .NotEmpty().WithMessage("Route ID is required.");
    }
}
