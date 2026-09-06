using FluentValidation;

namespace TaxiPayApi.Application.Features.Drivers.Commands.UpdateProfile;

public class UpdateDriverProfileCommandValidator : AbstractValidator<UpdateDriverProfileCommand>
{
    public UpdateDriverProfileCommandValidator()
    {
        RuleFor(x => x.DriverId)
            .NotEmpty().WithMessage("Driver ID is required.");

        RuleFor(x => x.Request.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MaximumLength(50).WithMessage("First name must not exceed 50 characters.");

        RuleFor(x => x.Request.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MaximumLength(50).WithMessage("Last name must not exceed 50 characters.");

        RuleFor(x => x.Request.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^0[79]\d{8}$").WithMessage("Phone number must be a valid Ethiopian format (09XXXXXXXX or 07XXXXXXXX).");

        RuleFor(x => x.Request.PhotoUrl)
            .MaximumLength(255).WithMessage("Photo URL must not exceed 255 characters.")
            .Must(url => string.IsNullOrEmpty(url) || Uri.IsWellFormedUriString(url, UriKind.Absolute))
            .WithMessage("Photo URL must be a valid URL.");
    }
}