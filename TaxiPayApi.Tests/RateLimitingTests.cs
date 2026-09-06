using System.Net;
using System.Security.Claims;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using TaxiPayApi.Api.Extensions;

namespace TaxiPayApi.Tests;

public class RateLimitingTests
{
    [Fact]
    public async Task RateLimiter_WhenPermitExceeded_Returns429TooManyRequestsWithProblemDetails()
    {
        // Arrange: Create a minimal test server configured with AddTaxiPayRateLimiting
        var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddRouting();
                        services.AddTaxiPayRateLimiting();
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseRateLimiter();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/test-auth", () => Results.Ok("authenticated"))
                                     .RequireRateLimiting("auth-strict");
                        });
                    });
            })
            .StartAsync();

        var client = host.GetTestClient();

        // Act: auth-strict allows 5 requests per window. The 6th should return 429.
        HttpResponseMessage? lastResponse = null;
        for (int i = 0; i < 6; i++)
        {
            lastResponse = await client.GetAsync("/test-auth");
        }

        // Assert
        Assert.NotNull(lastResponse);
        Assert.Equal((HttpStatusCode)429, lastResponse.StatusCode);
        Assert.True(lastResponse.Headers.Contains("Retry-After"));

        var content = await lastResponse.Content.ReadAsStringAsync();
        Assert.Contains("Too Many Requests", content);
        Assert.Contains("429", content);
    }

    [Fact]
    public async Task RateLimiter_FinancialStrictPolicy_Allows10Requests_AndRejects11thWith429()
    {
        // Arrange
        var host = await new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer()
                    .ConfigureServices(services =>
                    {
                        services.AddRouting();
                        services.AddTaxiPayRateLimiting();
                    })
                    .Configure(app =>
                    {
                        app.UseRouting();
                        app.UseRateLimiter();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapPost("/test-pay", () => Results.Ok(new { status = "paid" }))
                                     .RequireRateLimiting("financial-strict");
                        });
                    });
            })
            .StartAsync();

        var client = host.GetTestClient();

        // Act & Assert: First 10 requests should succeed (200 OK)
        for (int i = 0; i < 10; i++)
        {
            var res = await client.PostAsync("/test-pay", null);
            Assert.Equal(HttpStatusCode.OK, res.StatusCode);
        }

        // 11th request must be rejected with 429 Too Many Requests
        var rejectedResponse = await client.PostAsync("/test-pay", null);
        Assert.Equal((HttpStatusCode)429, rejectedResponse.StatusCode);
        Assert.True(rejectedResponse.Headers.Contains("Retry-After"));
    }
}
