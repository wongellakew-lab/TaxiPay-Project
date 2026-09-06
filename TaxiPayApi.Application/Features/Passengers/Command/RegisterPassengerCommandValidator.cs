using FluentValidation;
using TaxiPayApi.Application.DTOs.Passengers;

namespace TaxiPayApi.Application.Features.Passengers.Commands.Register;

public class RegisterPassengerCommandValidator : AbstractValidator<RegisterPassengerCommand>
{
    public RegisterPassengerCommandValidator()
    {
        RuleFor(x => x.Request.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(50).WithMessage("First name must not exceed 50 characters.");

        RuleFor(x => x.Request.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(50).WithMessage("Last name must not exceed 50 characters.");

        RuleFor(x => x.Request.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^0[79]\d{8}$").WithMessage("Phone number must be a valid Ethiopian format (09XXXXXXXX or 07XXXXXXXX).");

        RuleFor(x => x.Request.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters long.");
    }
}