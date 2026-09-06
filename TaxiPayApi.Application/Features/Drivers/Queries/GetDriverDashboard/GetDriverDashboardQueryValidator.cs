using FluentValidation;

namespace TaxiPayApi.Application.Features.Drivers.Queries.GetDashboard;

public class GetDriverDashboardQueryValidator : AbstractValidator<GetDriverDashboardQuery>
{
    public GetDriverDashboardQueryValidator()
    {
        RuleFor(x => x.DriverId)
            .NotEmpty().WithMessage("Driver ID is required.");
    }
}