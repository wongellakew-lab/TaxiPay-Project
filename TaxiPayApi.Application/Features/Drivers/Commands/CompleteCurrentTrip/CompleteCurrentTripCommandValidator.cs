using FluentValidation;

namespace TaxiPayApi.Application.Features.Drivers.Commands.CompleteCurrentTrip;

public class CompleteCurrentTripCommandValidator : AbstractValidator<CompleteCurrentTripCommand>
{
    public CompleteCurrentTripCommandValidator()
    {
        RuleFor(x => x.ShiftId)
            .NotEmpty().WithMessage("Shift ID is required.");

        RuleFor(x => x.DriverLatitude)
            .InclusiveBetween(-90, 90).WithMessage("Latitude must be between -90 and 90.");

        RuleFor(x => x.DriverLongitude)
            .InclusiveBetween(-180, 180).WithMessage("Longitude must be between -180 and 180.");
    }
}