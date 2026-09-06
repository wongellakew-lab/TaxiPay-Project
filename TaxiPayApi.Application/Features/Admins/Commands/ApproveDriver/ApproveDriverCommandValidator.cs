using FluentValidation;

namespace TaxiPayApi.Application.Features.Admins.Commands.ApproveDriver;

public class ApproveDriverCommandValidator : AbstractValidator<ApproveDriverCommand>
{
    public ApproveDriverCommandValidator()
    {
        RuleFor(x => x.DriverId)
            .NotEmpty().WithMessage("Driver ID is required.");
    }
}
