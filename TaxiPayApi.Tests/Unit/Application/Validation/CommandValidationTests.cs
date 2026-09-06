using FluentValidation.TestHelper;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Application.DTOs.Passengers;
using TaxiPayApi.Application.Features.Admins.Commands.CreateRoute;
using TaxiPayApi.Application.Features.Drivers.Commands.CreateDriver;
using TaxiPayApi.Application.Features.Passengers.Commands.PayTrip;
using TaxiPayApi.Application.Features.Passengers.Commands.TopUpWallet;
using TaxiPayApi.Domain.Entities;

namespace TaxiPayApi.Tests.Unit.Application.Validation;

public class CommandValidationTests
{
    private readonly CreateDriverCommandValidator _createDriverValidator = new();
    private readonly PayTripCommandValidator _payTripValidator = new();
    private readonly TopUpWalletCommandValidator _topUpValidator = new();
    private readonly CreateRouteCommandValidator _createRouteValidator = new();

    [Theory]
    [InlineData("0911223344", true)]
    [InlineData("0711223344", true)]
    [InlineData("0988776655", true)]
    [InlineData("0788776655", true)]
    [InlineData("0811223344", false)] // Invalid prefix
    [InlineData("12345", false)]       // Too short
    [InlineData("", false)]            // Empty
    public void CreateDriver_PhoneValidation_EnforcesEthiopianPhoneRules(string phone, bool isValid)
    {
        // Arrange
        var request = new CreateDriverRequest
        {
            FirstName = "Abebe",
            LastName = "Bikila",
            Phone = phone,
            LicenseNumber = "DL-12345",
            Password = "Password123!"
        };
        var command = new CreateDriverCommand(request);

        // Act
        var result = _createDriverValidator.TestValidate(command);

        // Assert
        if (isValid)
            result.ShouldNotHaveValidationErrorFor(x => x.Request.Phone);
        else
            result.ShouldHaveValidationErrorFor(x => x.Request.Phone);
    }

    [Theory]
    [InlineData(10.0, true)]
    [InlineData(500.0, true)]
    [InlineData(0.0, false)]    // Zero amount
    [InlineData(-50.0, false)]  // Negative amount
    public void TopUpWallet_AmountValidation_RequiresPositiveAmount(decimal amount, bool isValid)
    {
        // Arrange
        var request = new TopUpWalletRequest { Amount = amount, PaymentMethod = "Telebirr" };
        var command = new TopUpWalletCommand(Guid.NewGuid(), request);

        // Act
        var result = _topUpValidator.TestValidate(command);

        // Assert
        if (isValid)
            result.ShouldNotHaveValidationErrorFor(x => x.Request.Amount);
        else
            result.ShouldHaveValidationErrorFor(x => x.Request.Amount);
    }

    [Fact]
    public void CreateRoute_WhenFewerThanTwoNodes_FailsValidation()
    {
        // Arrange
        var request = new CreateRouteRequest(
            Name: "Megenagna - Tor Hailoch",
            RouteCode: "R-01",
            Nodes: new List<AdminNodeDto>
            {
                new("NodeA", "Megenagna", 9.01, 38.75)
            }
        );
        var command = new CreateRouteCommand(request);

        // Act
        var result = _createRouteValidator.TestValidate(command);

        // Assert
        result.ShouldHaveValidationErrorFor(x => x.Request.Nodes);
    }

    [Theory]
    [InlineData("ET1001", "A", "B", true)]
    [InlineData("", "A", "B", false)]        // Empty driver ID
    [InlineData("ET1001", "", "B", false)]   // Empty boarding node
    [InlineData("ET1001", "A", "", false)]   // Empty destination node
    [InlineData("ET1001", "A", "A", false)]  // Same boarding and destination
    public void PayTrip_RequiredFields_EnforcesCompleteness(
        string driverId, string boardingNode, string destinationNode, bool isValid)
    {
        // Arrange
        var request = new PayTripRequest
        {
            DriverId = driverId,
            BoardingNode = boardingNode,
            DestinationNode = destinationNode,
            PaymentMethod = PaymentMethod.Telebirr
        };
        var command = new PayTripCommand(Guid.NewGuid(), request);

        // Act
        var result = _payTripValidator.TestValidate(command);

        // Assert
        if (isValid)
            result.ShouldNotHaveAnyValidationErrors();
        else
            Assert.False(result.IsValid);
    }
}
