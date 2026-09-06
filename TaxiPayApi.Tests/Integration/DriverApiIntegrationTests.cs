using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Domain.Entities;
using TaxiPayApi.Tests.Fixtures;

namespace TaxiPayApi.Tests.Integration;

public class DriverApiIntegrationTests : IClassFixture<TaxiPayWebApplicationFactory>
{
    private readonly TaxiPayWebApplicationFactory _factory;

    public DriverApiIntegrationTests(TaxiPayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StartNewTrip_EndToEnd_Returns200AndPersistsActiveTripInDatabase()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var route = TestBuilders.CreateRoute();
        var driver = TestBuilders.CreateDriver(driverId, "ET-TX-9900", routeId: route.Id);
        var shift = TestBuilders.CreateShift(driver.Id, route.Id);

        await _factory.ExecuteDbContextAsync(async db =>
        {
            db.Routes.Add(route);
            db.Drivers.Add(driver);
            db.DriverShifts.Add(shift);
            await db.SaveChangesAsync();
        });

        var client = _factory.CreateDriverClient(driverId, "ET-TX-9900");
        var startRequest = new StartTripRequest("A→C");

        // Act: Driver starts new trip
        var response = await client.PostAsJsonAsync($"/api/drivers/shifts/{shift.Id}/trips/start", startRequest);

        // Assert HTTP
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TripResponse>();
        Assert.NotNull(result);
        Assert.Equal("Active", result.Status);

        // Assert Database State
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var dbShift = await db.DriverShifts.FindAsync(shift.Id);
            Assert.NotNull(dbShift);
            Assert.NotNull(dbShift.CurrentTripId);

            var dbTrip = await db.Trips.Where(t => t.DriverShiftId == shift.Id).FirstOrDefaultAsync();
            Assert.NotNull(dbTrip);
            Assert.Equal(TripStatus.Active, dbTrip.Status);
        });
    }

    [Fact]
    public async Task CompleteTrip_WhenPassengersAreOnboard_Returns400BadRequest()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var route = TestBuilders.CreateRoute();
        var driver = TestBuilders.CreateDriver(driverId, "ET-TX-9901", routeId: route.Id);
        var shift = TestBuilders.CreateShift(driver.Id, route.Id);
        var trip = TestBuilders.CreateTrip(shift.Id, driver.Id, route.Id, passengersCount: 1);
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

        var client = _factory.CreateDriverClient(driverId, "ET-TX-9901");
        var completeRequest = new CompleteTripRequest { Latitude = 9.0100m, Longitude = 38.7400m };

        // Act: Attempt to complete trip while passenger is still OnBoard
        var response = await client.PostAsJsonAsync($"/api/drivers/shifts/{shift.Id}/trips/complete", completeRequest);

        // Assert HTTP 400 Bad Request
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        // Verify Database State: Trip remains Active
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var dbTrip = await db.Trips.FindAsync(trip.Id);
            Assert.Equal(TripStatus.Active, dbTrip!.Status);
        });
    }

    [Fact]
    public async Task CompleteTrip_WhenAllPassengersAlighted_Returns200AndSetsStatusCompleted()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var route = TestBuilders.CreateRoute();
        var driver = TestBuilders.CreateDriver(driverId, "ET-TX-9902", routeId: route.Id);
        var shift = TestBuilders.CreateShift(driver.Id, route.Id);
        var trip = TestBuilders.CreateTrip(shift.Id, driver.Id, route.Id, passengersCount: 1);
        shift.CurrentTripId = trip.Id;

        var passenger = TestBuilders.CreatePassenger();
        var journey = TestBuilders.CreateJourney(trip.Id, passenger.Id, alightingStatus: AlightingStatus.Alighted);

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

        var client = _factory.CreateDriverClient(driverId, "ET-TX-9902");
        var completeRequest = new CompleteTripRequest { Latitude = 9.0100m, Longitude = 38.7400m };

        // Act
        var response = await client.PostAsJsonAsync($"/api/drivers/shifts/{shift.Id}/trips/complete", completeRequest);

        // Assert HTTP 200 OK
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Verify Database State: Trip is Completed
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var dbTrip = await db.Trips.FindAsync(trip.Id);
            Assert.Equal(TripStatus.Completed, dbTrip!.Status);
            Assert.NotNull(dbTrip.TripEnd);
        });
    }
}
