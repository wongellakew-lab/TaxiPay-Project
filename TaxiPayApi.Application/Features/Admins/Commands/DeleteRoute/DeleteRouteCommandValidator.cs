using FluentValidation;

namespace TaxiPayApi.Application.Features.Admins.Commands.DeleteRoute;

public class DeleteRouteCommandValidator : AbstractValidator<DeleteRouteCommand>
{
    public DeleteRouteCommandValidator()
    {
        RuleFor(x => x.RouteId)
            .NotEmpty().WithMessage("Route ID is required.");
    }
}
