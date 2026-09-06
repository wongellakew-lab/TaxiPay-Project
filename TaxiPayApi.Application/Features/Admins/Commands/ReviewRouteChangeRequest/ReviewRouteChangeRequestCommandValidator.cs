using FluentValidation;

namespace TaxiPayApi.Application.Features.Admins.Commands.ReviewRouteChangeRequest;

public class ReviewRouteChangeRequestCommandValidator : AbstractValidator<ReviewRouteChangeRequestCommand>
{
    public ReviewRouteChangeRequestCommandValidator()
    {
        RuleFor(x => x.RequestId)
            .NotEmpty().WithMessage("Route change request ID is required.");

        RuleFor(x => x.Request.Note)
            .MaximumLength(500).WithMessage("Note must not exceed 500 characters.")
            .When(x => x.Request.Note is not null);

        RuleFor(x => x.Request.Note)
            .NotEmpty().WithMessage("A rejection note is required when rejecting a request.")
            .When(x => !x.Request.Approved);
    }
}
