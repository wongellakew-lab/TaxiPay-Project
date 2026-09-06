using FluentValidation;

namespace TaxiPayApi.Application.Features.Drivers.Queries.GetProfile;

public class GetDriverProfileQueryValidator : AbstractValidator<GetDriverProfileQuery>
{
    public GetDriverProfileQueryValidator()
    {
        RuleFor(x => x.DriverId)
            .NotEmpty().WithMessage("Driver ID is required.");
    }
}