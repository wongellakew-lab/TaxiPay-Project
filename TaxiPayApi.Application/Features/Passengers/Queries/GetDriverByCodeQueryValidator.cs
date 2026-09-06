using FluentValidation;

namespace TaxiPayApi.Application.Features.Passengers.Queries.GetDriverByCode;

public class GetDriverByCodeQueryValidator : AbstractValidator<GetDriverByCodeQuery>
{
    public GetDriverByCodeQueryValidator()
    {
        RuleFor(x => x.DriverId)
            .NotEmpty().WithMessage("Driver ID is required.")
            .Length(6).WithMessage("Driver ID must be exactly 6 characters.")
            .Matches(@"^[A-Z0-9]{6}$").WithMessage("Driver ID must be 6 alphanumeric characters.");
    }
}