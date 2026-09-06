using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaxiPayApi.Api.Middlewares;
using TaxiPayApi.Application.Interfaces;
using TaxiPayApi.Domain.Entities;
using TaxiPayApi.Infrastructure.Security.Handlers;
using TaxiPayApi.Infrastructure.Security.Requirements;
using TaxiPayApi.Infrastructure.Services;
using Xunit;

namespace TaxiPayApi.Tests;

public class SecurityAndAuthorizationTests
{
    [Fact]
    public async Task DriverOwnsTripHandler_WhenDriverOwnsShift_Succeeds()
    {
        // Arrange
        var driverGuid = Guid.NewGuid();
        var handler = new DriverOwnsTripAuthorizationHandler();
        var requirement = new DriverOwnsTripRequirement();

        var claims = new[]
        {
            new Claim(ClaimTypes.Role, "Driver"),
            new Claim("driverGuid", driverGuid.ToString())
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var shift = new DriverShift { DriverId = driverGuid };

        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            shift);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task DriverOwnsTripHandler_WhenDriverDoesNotOwnShift_DoesNotSucceed()
    {
        // Arrange
        var driverA = Guid.NewGuid();
        var driverB = Guid.NewGuid();
        var handler = new DriverOwnsTripAuthorizationHandler();
        var requirement = new DriverOwnsTripRequirement();

        var claims = new[]
        {
            new Claim(ClaimTypes.Role, "Driver"),
            new Claim("driverGuid", driverA.ToString())
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var shift = new DriverShift { DriverId = driverB };

        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            shift);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task DriverOwnsTripHandler_WhenAdmin_AlwaysSucceeds()
    {
        // Arrange
        var driverB = Guid.NewGuid();
        var handler = new DriverOwnsTripAuthorizationHandler();
        var requirement = new DriverOwnsTripRequirement();

        var claims = new[]
        {
            new Claim(ClaimTypes.Role, "Admin")
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var shift = new DriverShift { DriverId = driverB };

        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            shift);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task PassengerResourceHandler_WhenPassengerOwnsResource_Succeeds()
    {
        // Arrange
        var passengerGuid = Guid.NewGuid();
        var handler = new PassengerResourceAuthorizationHandler();
        var requirement = new PassengerOwnsResourceRequirement();

        var claims = new[]
        {
            new Claim(ClaimTypes.Role, "Passenger"),
            new Claim("passengerId", passengerGuid.ToString())
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            passengerGuid);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task PassengerResourceHandler_WhenPassengerAccessesOtherPassenger_Fails()
    {
        // Arrange
        var passengerA = Guid.NewGuid();
        var passengerB = Guid.NewGuid();
        var handler = new PassengerResourceAuthorizationHandler();
        var requirement = new PassengerOwnsResourceRequirement();

        var claims = new[]
        {
            new Claim(ClaimTypes.Role, "Passenger"),
            new Claim("passengerId", passengerA.ToString())
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            passengerB);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.False(context.HasSucceeded);
    }

    [Fact]
    public async Task PassengerJourneyHandler_WhenPassengerOwnsJourney_Succeeds()
    {
        // Arrange
        var passengerGuid = Guid.NewGuid();
        var handler = new PassengerJourneyAuthorizationHandler();
        var requirement = new PassengerOwnsJourneyRequirement();

        var claims = new[]
        {
            new Claim(ClaimTypes.Role, "Passenger"),
            new Claim("passengerId", passengerGuid.ToString())
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));
        var journey = new PassengerJourney { PassengerId = passengerGuid };

        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            journey);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public async Task DriverResourceHandler_WhenDriverMatches_Succeeds()
    {
        // Arrange
        var driverGuid = Guid.NewGuid();
        var handler = new DriverResourceAuthorizationHandler();
        var requirement = new DriverOwnsResourceRequirement();

        var claims = new[]
        {
            new Claim(ClaimTypes.Role, "Driver"),
            new Claim("driverGuid", driverGuid.ToString())
        };
        var user = new ClaimsPrincipal(new ClaimsIdentity(claims, "TestAuth"));

        var context = new AuthorizationHandlerContext(
            new[] { requirement },
            user,
            driverGuid);

        // Act
        await handler.HandleAsync(context);

        // Assert
        Assert.True(context.HasSucceeded);
    }

    [Fact]
    public void CurrentUserService_WhenGivenDriverPrincipal_ResolvesClaimsCorrectly()
    {
        // Arrange
        var driverGuid = Guid.NewGuid();
        var identityId = Guid.NewGuid().ToString();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, identityId),
            new Claim(ClaimTypes.Role, "Driver"),
            new Claim("driverGuid", driverGuid.ToString()),
            new Claim("driverId", "ET-TX-001")
        }, "TestAuth"));

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var currentUserService = new CurrentUserService(accessor);

        // Act & Assert
        Assert.True(currentUserService.IsAuthenticated);
        Assert.True(currentUserService.IsDriver);
        Assert.False(currentUserService.IsAdmin);
        Assert.False(currentUserService.IsPassenger);
        Assert.Equal(driverGuid, currentUserService.DriverGuid);
        Assert.Equal("ET-TX-001", currentUserService.DriverCode);
        Assert.Equal(identityId, currentUserService.UserId);
    }

    [Fact]
    public void CurrentUserService_WhenGivenPassengerPrincipal_ResolvesClaimsCorrectly()
    {
        // Arrange
        var passengerGuid = Guid.NewGuid();
        var identityId = Guid.NewGuid().ToString();
        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, identityId),
            new Claim(ClaimTypes.Role, "Passenger"),
            new Claim("passengerId", passengerGuid.ToString())
        }, "TestAuth"));

        var accessor = new HttpContextAccessor { HttpContext = httpContext };
        var currentUserService = new CurrentUserService(accessor);

        // Act & Assert
        Assert.True(currentUserService.IsAuthenticated);
        Assert.True(currentUserService.IsPassenger);
        Assert.False(currentUserService.IsDriver);
        Assert.False(currentUserService.IsAdmin);
        Assert.Equal(passengerGuid, currentUserService.PassengerGuid);
    }

    [Fact]
    public async Task SecurityHeadersMiddleware_AddsHardeningHeaders()
    {
        // Arrange
        var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddRouting();
                    })
                    .Configure(app =>
                    {
                        app.UseMiddleware<SecurityHeadersMiddleware>();
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/test-headers", () => Results.Ok("secured"));
                        });
                    });
            })
            .StartAsync();

        var client = host.GetTestClient();

        // Act
        var response = await client.GetAsync("/test-headers");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.True(response.Headers.Contains("X-Content-Type-Options"));
        Assert.Equal("nosniff", response.Headers.GetValues("X-Content-Type-Options").First());

        Assert.True(response.Headers.Contains("X-Frame-Options"));
        Assert.Equal("DENY", response.Headers.GetValues("X-Frame-Options").First());

        Assert.True(response.Headers.Contains("Referrer-Policy"));
        Assert.Equal("strict-origin-when-cross-origin", response.Headers.GetValues("Referrer-Policy").First());

        Assert.True(response.Headers.Contains("Content-Security-Policy"));
        var csp = response.Headers.GetValues("Content-Security-Policy").First();
        Assert.Contains("default-src 'self'", csp);
        Assert.Contains("frame-ancestors 'none'", csp);
    }
}
