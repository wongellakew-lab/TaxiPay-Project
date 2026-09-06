using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Application.Services;
using TaxiPayApi.Domain.Entities;
using Xunit;

namespace TaxiPayApi.Tests;

public class DriverDirectionAndBreakdownTests
{
    private readonly Route _testRoute;

    public DriverDirectionAndBreakdownTests()
    {
        _testRoute = new Route
        {
            Id = Guid.NewGuid(),
            RouteCode = "R01",
            Name = "Bole - Merkato Corridor",
            Nodes = new List<Node>
            {
                new() { Id = "A", Name = "Bole Medhanealem", Latitude = 8.995, Longitude = 38.788 },
                new() { Id = "B", Name = "Atlas Hotel", Latitude = 9.002, Longitude = 38.780 },
                new() { Id = "C", Name = "Meskel Square", Latitude = 9.010, Longitude = 38.763 },
                new() { Id = "D", Name = "Mexico Square", Latitude = 9.011, Longitude = 38.745 },
                new() { Id = "E", Name = "Teklehaimanot", Latitude = 9.023, Longitude = 38.739 },
                new() { Id = "F", Name = "Merkato Bus Terminal", Latitude = 9.031, Longitude = 38.734 }
            }
        };
    }

    [Fact]
    public void FormatDirectionDisplay_ShouldResolveOutboundEndpoints_WhenDirectionIsPointIds()
    {
        // Act
        var result = DriverService.FormatDirectionDisplay("A→F", _testRoute);

        // Assert
        Assert.Equal("Bole Medhanealem → Merkato Bus Terminal", result);
    }

    [Fact]
    public void FormatDirectionDisplay_ShouldResolveInboundEndpoints_WhenDirectionIsReverse()
    {
        // Act
        var result = DriverService.FormatDirectionDisplay("F→A", _testRoute);

        // Assert
        Assert.Equal("Merkato Bus Terminal → Bole Medhanealem", result);
    }

    [Fact]
    public void FormatDirectionDisplay_ShouldResolveForwardKeyword_ToStartAndEndNodes()
    {
        // Act
        var result = DriverService.FormatDirectionDisplay("FORWARD", _testRoute);

        // Assert
        Assert.Equal("Bole Medhanealem → Merkato Bus Terminal", result);
    }

    [Fact]
    public void FormatDirectionDisplay_ShouldResolveReverseKeyword_ToEndAndStartNodes()
    {
        // Act
        var result = DriverService.FormatDirectionDisplay("REVERSE", _testRoute);

        // Assert
        Assert.Equal("Merkato Bus Terminal → Bole Medhanealem", result);
    }

    [Fact]
    public void FormatDirectionDisplay_ShouldFallbackGracefully_WhenRouteIsNull()
    {
        // Act
        var result = DriverService.FormatDirectionDisplay("A-B", null);

        // Assert
        Assert.Equal("A → B", result);
    }

    [Fact]
    public void PaymentBreakdownDto_ShouldCarryDigitalAndCashAuthoritativeAmounts()
    {
        // Arrange
        var telebirr = 300.00m;
        var wallet = 150.00m;
        var cash = 50.00m;

        // Act
        var breakdown = new PaymentBreakdownDto
        {
            Telebirr = telebirr,
            Wallet = wallet,
            Digital = telebirr + wallet,
            Cash = cash
        };

        // Assert
        Assert.Equal(450.00m, breakdown.Digital);
        Assert.Equal(50.00m, breakdown.Cash);
        Assert.Equal(300.00m, breakdown.Telebirr);
        Assert.Equal(150.00m, breakdown.Wallet);
    }
}
