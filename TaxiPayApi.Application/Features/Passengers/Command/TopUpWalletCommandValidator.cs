using FluentValidation;

namespace TaxiPayApi.Application.Features.Passengers.Commands.TopUpWallet;

public class TopUpWalletCommandValidator : AbstractValidator<TopUpWalletCommand>
{
    public TopUpWalletCommandValidator()
    {
        RuleFor(x => x.PassengerId)
            .NotEmpty().WithMessage("Passenger ID is required.");

        RuleFor(x => x.Request.Amount)
            .GreaterThan(0).WithMessage("Top-up amount must be greater than zero.")
            .LessThanOrEqualTo(50000).WithMessage("Top-up amount cannot exceed 50,000 ETB.");
    }
}
