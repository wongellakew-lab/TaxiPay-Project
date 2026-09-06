using FluentValidation;

namespace TaxiPayApi.Application.Features.Passengers.Queries.GetTripHistory;

public class GetPassengerTripHistoryQueryValidator : AbstractValidator<GetPassengerTripHistoryQuery>
{
    public GetPassengerTripHistoryQueryValidator()
    {
        RuleFor(x => x.PassengerId)
            .NotEmpty().WithMessage("Passenger ID is required.");

        RuleFor(x => x.PageNumber)
            .GreaterThanOrEqualTo(1).WithMessage("Page number must be at least 1.");

        RuleFor(x => x.PageSize)
            .InclusiveBetween(1, 50).WithMessage("Page size must be between 1 and 50.");
    }
}