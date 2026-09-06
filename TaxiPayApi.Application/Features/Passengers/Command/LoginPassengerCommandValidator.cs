using FluentValidation;
using TaxiPayApi.Application.DTOs.Passengers;

namespace TaxiPayApi.Application.Features.Passengers.Commands.Login;

public class LoginPassengerCommandValidator : AbstractValidator<LoginPassengerCommand>
{
    public LoginPassengerCommandValidator()
    {
        RuleFor(x => x.Request.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^0[79]\d{8}$").WithMessage("Invalid phone number format.");

        RuleFor(x => x.Request.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}