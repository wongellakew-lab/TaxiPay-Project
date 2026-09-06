using FluentValidation;

namespace TaxiPayApi.Application.Features.Admins.Commands.ResolveDispute;

public class ResolveDisputeCommandValidator : AbstractValidator<ResolveDisputeCommand>
{
    private static readonly string[] AllowedStatuses = ["Resolved", "Rejected"];

    public ResolveDisputeCommandValidator()
    {
        RuleFor(x => x.DisputeId)
            .NotEmpty().WithMessage("Dispute ID is required.");

        RuleFor(x => x.Request.Status)
            .NotEmpty().WithMessage("Resolution status is required.")
            .Must(s => AllowedStatuses.Contains(s))
            .WithMessage($"Status must be one of: {string.Join(", ", AllowedStatuses)}.");

        RuleFor(x => x.Request.ResolutionNotes)
            .NotEmpty().WithMessage("Resolution notes are required.")
            .MinimumLength(10).WithMessage("Resolution notes must be at least 10 characters.")
            .MaximumLength(1000).WithMessage("Resolution notes must not exceed 1,000 characters.");
    }
}
