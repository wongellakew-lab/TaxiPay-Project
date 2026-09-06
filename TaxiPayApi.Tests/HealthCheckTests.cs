using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using TaxiPayApi.Api.Extensions;

namespace TaxiPayApi.Tests;

public class HealthCheckTests
{
    [Fact]
    public async Task WriteLiveResponse_OutputsMinimalHealthyStatusWithoutSensitiveData()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var report = new HealthReport(new Dictionary<string, HealthReportEntry>(), TimeSpan.FromMilliseconds(5));

        // Act
        await HealthCheckResponseWriter.WriteLiveResponse(context, report);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("Healthy", root.GetProperty("status").GetString());
        Assert.Equal("TaxiPayApi", root.GetProperty("service").GetString());
        Assert.True(root.TryGetProperty("timestamp", out _));

        // Verify no sensitive internal strings leaked
        Assert.DoesNotContain("Host=", json);
        Assert.DoesNotContain("Password=", json);
        Assert.DoesNotContain("Secret", json);
    }

    [Fact]
    public async Task WriteResponse_OutputsStructuredEntries()
    {
        // Arrange
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var entries = new Dictionary<string, HealthReportEntry>
        {
            ["PostgreSQL"] = new(HealthStatus.Healthy, "Database is responsive", TimeSpan.FromMilliseconds(10), null, null, ["ready", "db"])
        };
        var report = new HealthReport(entries, TimeSpan.FromMilliseconds(12));

        // Act
        await HealthCheckResponseWriter.WriteResponse(context, report);

        // Assert
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var json = await reader.ReadToEndAsync();

        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;

        Assert.Equal("Healthy", root.GetProperty("status").GetString());
        var entriesElem = root.GetProperty("entries");
        Assert.True(entriesElem.TryGetProperty("PostgreSQL", out var dbElem));
        Assert.Equal("Healthy", dbElem.GetProperty("status").GetString());
        Assert.Equal("Database is responsive", dbElem.GetProperty("description").GetString());
    }
}
