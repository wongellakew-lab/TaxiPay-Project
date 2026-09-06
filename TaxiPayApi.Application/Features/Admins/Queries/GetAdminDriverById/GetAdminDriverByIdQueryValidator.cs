using FluentValidation;
using TaxiPayApi.Application.Features.Admins.Queries.GetAdminDriverById;

namespace TaxiPayApi.Application.Features.Admins.Queries.GetAdminDriverById;

public class GetAdminDriverByIdQueryValidator : AbstractValidator<GetAdminDriverByIdQuery>
{
    public GetAdminDriverByIdQueryValidator()
    {
        RuleFor(x => x.DriverId)
            .NotEmpty().WithMessage("Driver ID is required.");
    }
}
