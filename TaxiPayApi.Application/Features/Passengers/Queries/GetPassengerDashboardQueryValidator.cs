using FluentValidation;

namespace TaxiPayApi.Application.Features.Passengers.Queries.GetDashboard;

public class GetPassengerDashboardQueryValidator : AbstractValidator<GetPassengerDashboardQuery>
{
    public GetPassengerDashboardQueryValidator()
    {
        RuleFor(x => x.PassengerId)
            .NotEmpty().WithMessage("Passenger ID is required.");
    }
}