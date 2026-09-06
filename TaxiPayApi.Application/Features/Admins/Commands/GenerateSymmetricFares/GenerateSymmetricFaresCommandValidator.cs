using FluentValidation;

namespace TaxiPayApi.Application.Features.Admins.Commands.GenerateSymmetricFares;

public class GenerateSymmetricFaresCommandValidator : AbstractValidator<GenerateSymmetricFaresCommand>
{
    public GenerateSymmetricFaresCommandValidator()
    {
        RuleFor(x => x.Request.RouteId)
            .NotEmpty().WithMessage("Route ID is required.");

        RuleFor(x => x.Request.BaseFarePerHop)
            .GreaterThan(0).WithMessage("Base fare per hop must be greater than zero.")
            .LessThanOrEqualTo(5000).WithMessage("Base fare per hop must not exceed 5,000.");
    }
}
