using FluentValidation;
using TaxiPayApi.Application.DTOs.Passengers;

namespace TaxiPayApi.Application.Features.Passengers.Commands.PayTrip;

public class PayTripCommandValidator : AbstractValidator<PayTripCommand>
{
    public PayTripCommandValidator()
    {
        RuleFor(x => x.PassengerId)
            .NotEmpty().WithMessage("Passenger ID is required.");

        RuleFor(x => x.Request.DriverId)
            .NotEmpty().WithMessage("Driver ID is required.")
            .Length(6).WithMessage("Driver ID must be exactly 6 characters.");

        RuleFor(x => x.Request.BoardingNode)
            .NotEmpty().WithMessage("Boarding node is required.")
            .Length(1).WithMessage("Boarding node must be a single character.");

        RuleFor(x => x.Request.DestinationNode)
            .NotEmpty().WithMessage("Destination node is required.")
            .Length(1).WithMessage("Destination node must be a single character.");

        RuleFor(x => x.Request)
            .Must(x => x.BoardingNode != x.DestinationNode)
            .WithMessage("Boarding and destination nodes cannot be the same.");

        RuleFor(x => x.Request.PaymentMethod)
            .IsInEnum().WithMessage("Invalid payment method.");
    }
}