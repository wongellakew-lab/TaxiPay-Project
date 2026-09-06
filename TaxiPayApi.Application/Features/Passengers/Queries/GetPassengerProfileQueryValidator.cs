using FluentValidation;

namespace TaxiPayApi.Application.Features.Passengers.Queries.GetProfile;

public class GetPassengerProfileQueryValidator : AbstractValidator<GetPassengerProfileQuery>
{
    public GetPassengerProfileQueryValidator()
    {
        RuleFor(x => x.PassengerId)
            .NotEmpty().WithMessage("Passenger ID is required.");
    }
}