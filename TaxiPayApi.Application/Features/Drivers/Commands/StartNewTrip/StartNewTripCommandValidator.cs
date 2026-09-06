using FluentValidation;

namespace TaxiPayApi.Application.Features.Drivers.Commands.StartNewTrip;

public class StartNewTripCommandValidator : AbstractValidator<StartNewTripCommand>
{
    public StartNewTripCommandValidator()
    {
        RuleFor(x => x.ShiftId)
            .NotEmpty().WithMessage("Shift ID is required.");

        RuleFor(x => x.Direction)
            .NotEmpty().WithMessage("Direction is required.")
            .Matches(@"^[A-Z]+→[A-Z]+$").WithMessage("Direction must be in format 'A→F' or 'F→A'.");
    }
}