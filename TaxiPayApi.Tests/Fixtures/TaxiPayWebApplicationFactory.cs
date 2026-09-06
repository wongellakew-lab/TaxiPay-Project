using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Npgsql;
using TaxiPayApi.Application.Interfaces;
using TaxiPayApi.Infrastructure.Persistence.Context;

namespace TaxiPayApi.Tests.Fixtures;

public class TaxiPayWebApplicationFactory : WebApplicationFactory<Program>
{
    public string DatabaseName { get; } = $"TaxiPayTestDb_{Guid.NewGuid():N}";
    public TestRealtimeNotifier RealtimeNotifier { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registrations
            var dbContextDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<TaxiPayDbContext>));
            if (dbContextDescriptor != null)
            {
                services.Remove(dbContextDescriptor);
            }

            var npgsqlDataSourceDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(NpgsqlDataSource));
            if (npgsqlDataSourceDescriptor != null)
            {
                services.Remove(npgsqlDataSourceDescriptor);
            }

            // Register isolated in-memory test database
            services.AddDbContext<TaxiPayDbContext>(options =>
            {
                options.UseInMemoryDatabase(DatabaseName);
                options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning));
                options.EnableSensitiveDataLogging();
            });

            // Replace Realtime Notifier with Test Spy
            var notifierDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(ITaxiPayRealtimeNotifier));
            if (notifierDescriptor != null)
            {
                services.Remove(notifierDescriptor);
            }
            services.AddSingleton<ITaxiPayRealtimeNotifier>(RealtimeNotifier);

            // Configure Test Authentication Scheme
            services.AddAuthentication(defaultScheme: TestAuthHandler.AuthenticationScheme)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(
                    TestAuthHandler.AuthenticationScheme, _ => { });

            services.PostConfigure<AuthenticationOptions>(options =>
            {
                options.DefaultAuthenticateScheme = TestAuthHandler.AuthenticationScheme;
                options.DefaultChallengeScheme = TestAuthHandler.AuthenticationScheme;
                options.DefaultForbidScheme = TestAuthHandler.AuthenticationScheme;
                options.DefaultScheme = TestAuthHandler.AuthenticationScheme;
            });
        });
    }

    public HttpClient CreateUnauthenticatedClient()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Unauthenticated", "true");
        return client;
    }

    public HttpClient CreatePassengerClient(Guid passengerId, string name = "Almaz Tesfaye", string phone = "+251911000000")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "Passenger");
        client.DefaultRequestHeaders.Add("X-Test-UserId", passengerId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-PassengerId", passengerId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Name", name);
        client.DefaultRequestHeaders.Add("X-Test-Phone", phone);
        return client;
    }

    public HttpClient CreateDriverClient(Guid driverId, string driverCode = "ET-TX-1001", string name = "Abebe Bikila", string phone = "+251911223344")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "Driver");
        client.DefaultRequestHeaders.Add("X-Test-UserId", driverId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-DriverId", driverId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-DriverCode", driverCode);
        client.DefaultRequestHeaders.Add("X-Test-Name", name);
        client.DefaultRequestHeaders.Add("X-Test-Phone", phone);
        return client;
    }

    public HttpClient CreateAdminClient(Guid adminId, string name = "Super Admin")
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Test-Role", "Admin");
        client.DefaultRequestHeaders.Add("X-Test-UserId", adminId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-AdminId", adminId.ToString());
        client.DefaultRequestHeaders.Add("X-Test-Name", name);
        return client;
    }

    public async Task ExecuteDbContextAsync(Func<TaxiPayDbContext, Task> action)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TaxiPayDbContext>();
        await action(context);
    }

    public async Task<T> ExecuteDbContextAsync<T>(Func<TaxiPayDbContext, Task<T>> action)
    {
        using var scope = Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<TaxiPayDbContext>();
        return await action(context);
    }
}
