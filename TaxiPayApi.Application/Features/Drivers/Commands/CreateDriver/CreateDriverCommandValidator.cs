using FluentValidation;
using TaxiPayApi.Application.DTOs;

namespace TaxiPayApi.Application.Features.Drivers.Commands.CreateDriver;

public class CreateDriverCommandValidator : AbstractValidator<CreateDriverCommand>
{
    public CreateDriverCommandValidator()
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

        RuleFor(x => x.Request.LicenseNumber)
            .NotEmpty().WithMessage("License number is required.")
            .MinimumLength(5).WithMessage("License number must be at least 5 characters.")
            .MaximumLength(20).WithMessage("License number must not exceed 20 characters.");

        RuleFor(x => x.Request.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.");
    }
}