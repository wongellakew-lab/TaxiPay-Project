using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Application.DTOs.Passengers;
using TaxiPayApi.Domain.Entities;
using TaxiPayApi.Tests.Fixtures;

namespace TaxiPayApi.Tests.Integration;

public class ApiContractTests : IClassFixture<TaxiPayWebApplicationFactory>
{
    private readonly TaxiPayWebApplicationFactory _factory;

    public ApiContractTests(TaxiPayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PaymentErrorContract_WhenInsufficientBalance_ReturnsStableErrorCode()
    {
        // Arrange
        var passengerId = Guid.NewGuid();
        var route = TestBuilders.CreateRoute();
        var fare = TestBuilders.CreateFare(route.Id, "A", "C", 100.00m);
        var driver = TestBuilders.CreateDriver(routeId: route.Id);
        var shift = TestBuilders.CreateShift(driver.Id, route.Id);
        var trip = TestBuilders.CreateTrip(shift.Id, driver.Id, route.Id);
        shift.CurrentTripId = trip.Id;
        var passenger = TestBuilders.CreatePassenger(passengerId, walletBalance: 10.00m);

        await _factory.ExecuteDbContextAsync(async db =>
        {
            db.Routes.Add(route);
            db.FareMatrices.Add(fare);
            db.Drivers.Add(driver);
            db.DriverShifts.Add(shift);
            db.Trips.Add(trip);
            db.Passengers.Add(passenger);
            await db.SaveChangesAsync();
        });

        var client = _factory.CreatePassengerClient(passengerId);
        var request = new PayTripRequest
        {
            DriverId = driver.DriverId!,
            BoardingNode = "A",
            DestinationNode = "C",
            PaymentMethod = PaymentMethod.Wallet
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/passengers/{passengerId}/pay", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("insufficient_balance", content);
    }

    [Fact]
    public async Task PaymentErrorContract_WhenDriverHasNoActiveTrip_ReturnsDriverNoActiveTrip()
    {
        // Arrange
        var passengerId = Guid.NewGuid();
        var route = TestBuilders.CreateRoute();
        var fare = TestBuilders.CreateFare(route.Id, "A", "C", 25.00m);
        var uniqueDriverCode = $"D{Guid.NewGuid():N}"[..6].ToUpper();
        var driver = TestBuilders.CreateDriver(code: uniqueDriverCode, routeId: route.Id);
        var shift = TestBuilders.CreateShift(driver.Id, route.Id);
        var passenger = TestBuilders.CreatePassenger(passengerId, walletBalance: 100.00m);

        await _factory.ExecuteDbContextAsync(async db =>
        {
            db.Routes.Add(route);
            db.FareMatrices.Add(fare);
            db.Drivers.Add(driver);
            db.DriverShifts.Add(shift);
            db.Passengers.Add(passenger);
            await db.SaveChangesAsync();
        });

        var client = _factory.CreatePassengerClient(passengerId);
        var request = new PayTripRequest
        {
            DriverId = driver.DriverId!,
            BoardingNode = "A",
            DestinationNode = "C",
            PaymentMethod = PaymentMethod.Wallet
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/passengers/{passengerId}/pay", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("driver_no_active_trip", content);
    }

    [Fact]
    public async Task CompleteTripErrorContract_WhenPassengersOnBoard_ReturnsCannotCompleteTrip()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var route = TestBuilders.CreateRoute();
        var driver = TestBuilders.CreateDriver(driverId, "ET1003", routeId: route.Id);
        var shift = TestBuilders.CreateShift(driver.Id, route.Id);
        var trip = TestBuilders.CreateTrip(shift.Id, driver.Id, route.Id);
        shift.CurrentTripId = trip.Id;

        var passenger = TestBuilders.CreatePassenger();
        var journey = TestBuilders.CreateJourney(trip.Id, passenger.Id, alightingStatus: AlightingStatus.OnBoard);

        await _factory.ExecuteDbContextAsync(async db =>
        {
            db.Routes.Add(route);
            db.Drivers.Add(driver);
            db.DriverShifts.Add(shift);
            db.Trips.Add(trip);
            db.Passengers.Add(passenger);
            db.PassengerJourneyss.Add(journey);
            await db.SaveChangesAsync();
        });

        var client = _factory.CreateDriverClient(driverId, "ET1003");
        var completeRequest = new CompleteTripRequest { Latitude = 9.0100m, Longitude = 38.7400m };

        // Act
        var response = await client.PostAsJsonAsync($"/api/drivers/shifts/{shift.Id}/trips/complete", completeRequest);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("cannot_complete_trip", content);
    }
}
