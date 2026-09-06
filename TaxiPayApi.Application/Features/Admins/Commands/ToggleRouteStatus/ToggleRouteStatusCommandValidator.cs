using FluentValidation;

namespace TaxiPayApi.Application.Features.Admins.Commands.ToggleRouteStatus;

public class ToggleRouteStatusCommandValidator : AbstractValidator<ToggleRouteStatusCommand>
{
    public ToggleRouteStatusCommandValidator()
    {
        RuleFor(x => x.RouteId)
            .NotEmpty().WithMessage("Route ID is required.");
    }
}
