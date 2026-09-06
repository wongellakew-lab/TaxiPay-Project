using FluentValidation;

namespace TaxiPayApi.Application.Features.Passengers.Commands.UpdateProfile;

public class UpdatePassengerProfileCommandValidator : AbstractValidator<UpdatePassengerProfileCommand>
{
    public UpdatePassengerProfileCommandValidator()
    {
        RuleFor(x => x.PassengerId)
            .NotEmpty().WithMessage("Passenger ID is required.");

        RuleFor(x => x.Request.FirstName)
            .NotEmpty().WithMessage("First name is required.")
            .MinimumLength(2).WithMessage("First name must be at least 2 characters long.")
            .MaximumLength(50).WithMessage("First name must not exceed 50 characters.");

        RuleFor(x => x.Request.LastName)
            .NotEmpty().WithMessage("Last name is required.")
            .MinimumLength(2).WithMessage("Last name must be at least 2 characters long.")
            .MaximumLength(50).WithMessage("Last name must not exceed 50 characters.");

        RuleFor(x => x.Request.Phone)
            .NotEmpty().WithMessage("Phone number is required.")
            .Matches(@"^(0[79]\d{8}|\+251[79]\d{8})$")
            .WithMessage("Please enter a valid Ethiopian phone number (e.g. 0911000000 or 0711000000).");
    }
}
