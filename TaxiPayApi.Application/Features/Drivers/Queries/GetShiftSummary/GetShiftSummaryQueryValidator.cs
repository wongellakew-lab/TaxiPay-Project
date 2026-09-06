using FluentValidation;

namespace TaxiPayApi.Application.Features.Drivers.Queries.GetShiftSummary;

public class GetShiftSummaryQueryValidator : AbstractValidator<GetShiftSummaryQuery>
{
    public GetShiftSummaryQueryValidator()
    {
        RuleFor(x => x.ShiftId)
            .NotEmpty().WithMessage("Shift ID is required.");
    }
}