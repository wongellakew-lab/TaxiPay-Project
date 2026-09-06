using FluentValidation;

namespace TaxiPayApi.Application.Features.Admins.Commands.AdminLogin;

public class AdminLoginCommandValidator : AbstractValidator<AdminLoginCommand>
{
    public AdminLoginCommandValidator()
    {
        RuleFor(x => x.Request.Email)
            .NotEmpty().WithMessage("Email is required.")
            .EmailAddress().WithMessage("A valid email address is required.");

        RuleFor(x => x.Request.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
