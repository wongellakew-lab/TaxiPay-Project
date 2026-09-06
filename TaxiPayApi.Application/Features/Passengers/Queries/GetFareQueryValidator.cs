using FluentValidation;

namespace TaxiPayApi.Application.Features.Passengers.Queries.GetFare;

public class GetFareQueryValidator : AbstractValidator<GetFareQuery>
{
    public GetFareQueryValidator()
    {
        RuleFor(x => x.RouteId)
            .NotEmpty().WithMessage("Route ID is required.");

        RuleFor(x => x.FromNode)
            .NotEmpty().WithMessage("From node is required.")
            .Length(1).WithMessage("From node must be a single character.");

        RuleFor(x => x.ToNode)
            .NotEmpty().WithMessage("To node is required.")
            .Length(1).WithMessage("To node must be a single character.");

        RuleFor(x => x)
            .Must(x => x.FromNode != x.ToNode)
            .WithMessage("From node and To node cannot be the same.");
    }
}