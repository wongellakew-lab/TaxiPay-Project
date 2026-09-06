using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using TaxiPayApi.Api.Middlewares;
using TaxiPayApi.Application.Common.Telemetry;

namespace TaxiPayApi.Tests;

public class OpenTelemetryTests
{
    [Fact]
    public void TaxiPayDiagnostics_ActivitySourceAndMeter_AreConfigured()
    {
        // Assert
        Assert.NotNull(TaxiPayDiagnostics.ActivitySource);
        Assert.Equal("TaxiPay.Api", TaxiPayDiagnostics.ActivitySource.Name);
        Assert.NotNull(TaxiPayDiagnostics.Meter);
        Assert.Equal("TaxiPay.Api", TaxiPayDiagnostics.Meter.Name);

        // Verify metrics can be recorded without exception
        TaxiPayDiagnostics.PaymentsTotal.Add(1, new KeyValuePair<string, object?>("status", "success"));
        TaxiPayDiagnostics.PaymentsAmount.Record(25.50);
        TaxiPayDiagnostics.TripsTotal.Add(1, new KeyValuePair<string, object?>("action", "start"));
        TaxiPayDiagnostics.CacheHitsTotal.Add(1, new KeyValuePair<string, object?>("tag", "routes"));
        TaxiPayDiagnostics.CacheMissesTotal.Add(1, new KeyValuePair<string, object?>("tag", "routes"));
        TaxiPayDiagnostics.RateLimitRejectedTotal.Add(1, new KeyValuePair<string, object?>("tier", "passenger"));
    }

    [Fact]
    public async Task RequestLoggingMiddleware_AttachesTraceIdAndCorrelationIdHeaders()
    {
        // Arrange
        var context = new DefaultHttpContext();
        var mockLogger = new Mock<ILogger<RequestLoggingMiddleware>>();
        var middleware = new RequestLoggingMiddleware(ctx => Task.CompletedTask, mockLogger.Object);

        // Act
        await middleware.InvokeAsync(context);

        // Assert
        Assert.True(context.Response.Headers.ContainsKey("X-Trace-Id"));
        Assert.True(context.Response.Headers.ContainsKey("X-Correlation-Id"));
        Assert.False(string.IsNullOrWhiteSpace(context.Response.Headers["X-Trace-Id"].ToString()));
        Assert.False(string.IsNullOrWhiteSpace(context.Response.Headers["X-Correlation-Id"].ToString()));
    }
}
