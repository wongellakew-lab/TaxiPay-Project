using FluentValidation;

namespace TaxiPayApi.Application.Features.Admins.Commands.CreateFareMatrix;

public class CreateFareMatrixCommandValidator : AbstractValidator<CreateFareMatrixCommand>
{
    public CreateFareMatrixCommandValidator()
    {
        RuleFor(x => x.Request.RouteId)
            .NotEmpty().WithMessage("Route ID is required.");

        RuleFor(x => x.Request.FromNode)
            .NotEmpty().WithMessage("From node is required.")
            .MaximumLength(50).WithMessage("From node must not exceed 50 characters.");

        RuleFor(x => x.Request.ToNode)
            .NotEmpty().WithMessage("To node is required.")
            .MaximumLength(50).WithMessage("To node must not exceed 50 characters.");

        RuleFor(x => x.Request)
            .Must(r => r.FromNode != r.ToNode)
            .WithMessage("From node and To node must be different.");

        RuleFor(x => x.Request.Fare)
            .GreaterThan(0).WithMessage("Fare must be greater than zero.")
            .LessThanOrEqualTo(10000).WithMessage("Fare must not exceed 10,000.");
    }
}
