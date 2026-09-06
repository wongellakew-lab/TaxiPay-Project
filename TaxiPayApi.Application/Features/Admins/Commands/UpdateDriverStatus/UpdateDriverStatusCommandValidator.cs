using FluentValidation;

namespace TaxiPayApi.Application.Features.Admins.Commands.UpdateDriverStatus;

public class UpdateDriverStatusCommandValidator : AbstractValidator<UpdateDriverStatusCommand>
{
    private static readonly string[] AllowedStatuses = ["Active", "Inactive", "Suspended", "Pending"];

    public UpdateDriverStatusCommandValidator()
    {
        RuleFor(x => x.DriverId)
            .NotEmpty().WithMessage("Driver ID is required.");

        RuleFor(x => x.Request.Status)
            .NotEmpty().WithMessage("Status is required.")
            .Must(s => AllowedStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", AllowedStatuses)}.");

        RuleFor(x => x.Request.Note)
            .MaximumLength(500).WithMessage("Note must not exceed 500 characters.")
            .When(x => x.Request.Note is not null);
    }
}
