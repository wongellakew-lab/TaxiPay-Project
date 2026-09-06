using FluentValidation;

namespace TaxiPayApi.Application.Features.Admins.Commands.UpdateRoute;

public class UpdateRouteCommandValidator : AbstractValidator<UpdateRouteCommand>
{
    public UpdateRouteCommandValidator()
    {
        RuleFor(x => x.RouteId)
            .NotEmpty().WithMessage("Route ID is required.");

        RuleFor(x => x.Request.Name)
            .NotEmpty().WithMessage("Route name is required.")
            .MaximumLength(100).WithMessage("Route name must not exceed 100 characters.");

        RuleFor(x => x.Request.RouteCode)
            .NotEmpty().WithMessage("Route code is required.")
            .MaximumLength(20).WithMessage("Route code must not exceed 20 characters.")
            .Matches(@"^[A-Z0-9\-]+$").WithMessage("Route code must contain only uppercase letters, digits, or hyphens.");

        RuleFor(x => x.Request.Nodes)
            .NotNull().WithMessage("Nodes are required.")
            .Must(n => n.Count >= 2).WithMessage("A route must have at least 2 nodes.");

        RuleForEach(x => x.Request.Nodes).ChildRules(node =>
        {
            node.RuleFor(n => n.Id).NotEmpty().WithMessage("Each node must have an ID.");
            node.RuleFor(n => n.Name).NotEmpty().WithMessage("Each node must have a name.");
            node.RuleFor(n => n.Latitude).InclusiveBetween(-90, 90).WithMessage("Node latitude must be between -90 and 90.");
            node.RuleFor(n => n.Longitude).InclusiveBetween(-180, 180).WithMessage("Node longitude must be between -180 and 180.");
        });
    }
}
