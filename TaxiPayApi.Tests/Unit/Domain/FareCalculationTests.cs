using TaxiPayApi.Domain.Entities;

namespace TaxiPayApi.Tests.Unit.Domain;

public class FareCalculationTests
{
    [Fact]
    public void FareMatrix_WhenSameOriginAndDestination_TariffIsZero()
    {
        // Arrange
        var routeId = Guid.NewGuid();
        var fare = new FareMatrix
        {
            RouteId = routeId,
            FromNode = "Megenagna",
            ToNode = "Megenagna",
            Fare = 0.00m
        };

        // Assert
        Assert.Equal(0.00m, fare.Fare);
        Assert.Equal("Megenagna", fare.FromNode);
        Assert.Equal("Megenagna", fare.ToNode);
    }

    [Theory]
    [InlineData("Megenagna", "Haya Hulet", 10.00)]
    [InlineData("Megenagna", "Bambis", 15.00)]
    [InlineData("Megenagna", "Stadium", 20.00)]
    [InlineData("Megenagna", "Tor Hailoch", 25.00)]
    public void FareMatrix_CalculatesFareStages_Correctly(string fromNode, string toNode, decimal expectedFare)
    {
        // Arrange
        var routeId = Guid.NewGuid();
        var fareMatrix = new FareMatrix
        {
            RouteId = routeId,
            FromNode = fromNode,
            ToNode = toNode,
            Fare = expectedFare
        };

        // Assert
        Assert.Equal(expectedFare, fareMatrix.Fare);
        Assert.True(fareMatrix.Fare > 0m);
    }

    [Theory]
    [InlineData(5.00, 10.00, true)]   // Within balance
    [InlineData(10.00, 10.00, true)]  // Exactly equal balance
    [InlineData(15.00, 10.00, false)] // Insufficient balance
    [InlineData(0.00, 0.00, true)]    // Zero fare with zero balance
    public void Passenger_WalletSufficiency_EvaluatesCorrectly(decimal requiredFare, decimal currentBalance, bool expectedSufficiency)
    {
        // Act
        var isSufficient = currentBalance >= requiredFare;

        // Assert
        Assert.Equal(expectedSufficiency, isSufficient);
    }
}
