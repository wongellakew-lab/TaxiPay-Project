using FluentValidation;

namespace TaxiPayApi.Application.Features.Drivers.Commands;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Request.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^0[79]\d{8}$").WithMessage("Invalid phone number format.");

        RuleFor(x => x.Request.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}