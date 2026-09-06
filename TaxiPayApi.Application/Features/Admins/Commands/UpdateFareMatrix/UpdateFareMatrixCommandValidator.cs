using FluentValidation;

namespace TaxiPayApi.Application.Features.Admins.Commands.UpdateFareMatrix;

public class UpdateFareMatrixCommandValidator : AbstractValidator<UpdateFareMatrixCommand>
{
    public UpdateFareMatrixCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Fare matrix ID is required.");

        RuleFor(x => x.Request.Fare)
            .GreaterThan(0).WithMessage("Fare must be greater than zero.")
            .LessThanOrEqualTo(10000).WithMessage("Fare must not exceed 10,000.");
    }
}
