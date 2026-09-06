using System.Net;
using System.Net.Http.Json;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Application.DTOs.Passengers;
using TaxiPayApi.Domain.Entities;
using TaxiPayApi.Tests.Fixtures;

namespace TaxiPayApi.Tests.Integration;

public class AuthorizationBoundaryIntegrationTests : IClassFixture<TaxiPayWebApplicationFactory>
{
    private readonly TaxiPayWebApplicationFactory _factory;

    public AuthorizationBoundaryIntegrationTests(TaxiPayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ProtectedEndpoints_WhenUnauthenticated_Returns401Unauthorized()
    {
        // Arrange
        var client = _factory.CreateUnauthenticatedClient();
        var randomId = Guid.NewGuid();

        // Act
        var res1 = await client.GetAsync($"/api/passengers/{randomId}/profile");
        var res2 = await client.GetAsync($"/api/drivers/{randomId}");
        var res3 = await client.PostAsync($"/api/passengers/{randomId}/pay", JsonContent.Create(new PayTripRequest()));
        var res4 = await client.PostAsync($"/api/admins/drivers/{randomId}/approve", JsonContent.Create(new ApproveDriverRequest(null)));

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, res1.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, res2.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, res3.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, res4.StatusCode);
    }

    [Fact]
    public async Task Passenger_WhenAccessingOtherPassengerProfile_Returns403Forbidden()
    {
        // Arrange
        var passengerAId = Guid.NewGuid();
        var passengerBId = Guid.NewGuid();

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var pA = TestBuilders.CreatePassenger(passengerAId, "PassengerA");
            var pB = TestBuilders.CreatePassenger(passengerBId, "PassengerB");
            db.Passengers.AddRange(pA, pB);
            await db.SaveChangesAsync();
        });

        // Act: Passenger A tries to access Passenger B's profile
        var clientA = _factory.CreatePassengerClient(passengerAId);
        var response = await clientA.GetAsync($"/api/passengers/{passengerBId}/profile");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Passenger_WhenToppingUpOtherPassengerWallet_Returns403Forbidden()
    {
        // Arrange
        var passengerAId = Guid.NewGuid();
        var passengerBId = Guid.NewGuid();

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var pA = TestBuilders.CreatePassenger(passengerAId, "PassengerA");
            var pB = TestBuilders.CreatePassenger(passengerBId, "PassengerB");
            db.Passengers.AddRange(pA, pB);
            await db.SaveChangesAsync();
        });

        var clientA = _factory.CreatePassengerClient(passengerAId);
        var topUpRequest = new TopUpWalletRequest { Amount = 100.00m, PaymentMethod = "Telebirr" };

        // Act: Passenger A attempts to top up Passenger B's wallet
        var response = await clientA.PostAsJsonAsync($"/api/passengers/{passengerBId}/topup", topUpRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Driver_WhenAccessingOtherDriverProfile_Returns403Forbidden()
    {
        // Arrange
        var driverAId = Guid.NewGuid();
        var driverBId = Guid.NewGuid();

        await _factory.ExecuteDbContextAsync(async db =>
        {
            var dA = TestBuilders.CreateDriver(driverAId, "ET-TX-0001", "Driver", "A");
            var dB = TestBuilders.CreateDriver(driverBId, "ET-TX-0002", "Driver", "B");
            db.Drivers.AddRange(dA, dB);
            await db.SaveChangesAsync();
        });

        var clientA = _factory.CreateDriverClient(driverAId, "ET-TX-0001");

        // Act: Driver A attempts to view Driver B's profile
        var response = await clientA.GetAsync($"/api/drivers/{driverBId}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Driver_WhenCompletingOtherDriverTrip_Returns403Forbidden()
    {
        // Arrange
        var driverAId = Guid.NewGuid();
        var driverBId = Guid.NewGuid();
        var route = TestBuilders.CreateRoute();
        var driverB = TestBuilders.CreateDriver(driverBId, "ET1002");
        var shiftB = TestBuilders.CreateShift(driverBId, route.Id);

        await _factory.ExecuteDbContextAsync(async db =>
        {
            db.Routes.Add(route);
            db.Drivers.Add(driverB);
            db.DriverShifts.Add(shiftB);
            await db.SaveChangesAsync();
        });

        var clientA = _factory.CreateDriverClient(driverAId, "ET-TX-0001");
        var completeRequest = new CompleteTripRequest { Latitude = 9.0100m, Longitude = 38.7400m };

        // Act: Driver A attempts to complete Driver B's shift trip
        var response = await clientA.PostAsJsonAsync($"/api/drivers/shifts/{shiftB.Id}/trips/complete", completeRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Passenger_WhenCallingAdminEndpoint_Returns403Forbidden()
    {
        // Arrange
        var passengerId = Guid.NewGuid();
        var client = _factory.CreatePassengerClient(passengerId);

        // Act: Passenger calls an Admin endpoint
        var response = await client.PostAsJsonAsync($"/api/admins/drivers/{Guid.NewGuid()}/approve", new ApproveDriverRequest(null));

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Admin_WhenCallingAdminEndpoint_Returns200OrExpectedBusinessResult()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var route = TestBuilders.CreateRoute();
        var driver = TestBuilders.CreateDriver(status: DriverStatus.Pending);

        await _factory.ExecuteDbContextAsync(async db =>
        {
            db.Routes.Add(route);
            db.Drivers.Add(driver);
            await db.SaveChangesAsync();
        });

        var adminClient = _factory.CreateAdminClient(adminId);

        // Act: Admin approves driver
        var response = await adminClient.PostAsJsonAsync($"/api/admins/drivers/{driver.Id}/approve", new ApproveDriverRequest(route.Id));

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}
