using FluentValidation;

namespace TaxiPayApi.Application.Features.Drivers.Commands.RequestRouteChange;

public class RequestRouteChangeCommandValidator : AbstractValidator<RequestRouteChangeCommand>
{
    public RequestRouteChangeCommandValidator()
    {
        RuleFor(x => x.DriverId)
            .NotEmpty().WithMessage("Driver ID is required.");

        RuleFor(x => x.Request.EffectiveNewRouteId)
            .NotEmpty().WithMessage("New route ID is required.");

        RuleFor(x => x.Request.Reason)
            .NotEmpty().WithMessage("Reason is required.")
            .MaximumLength(500).WithMessage("Reason must not exceed 500 characters.");
    }
}