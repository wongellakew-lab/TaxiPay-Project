using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Domain.Entities;
using TaxiPayApi.Tests.Fixtures;

namespace TaxiPayApi.Tests.Integration.Trips;

public class TripLifecycleIntegrationTests : IClassFixture<TaxiPayWebApplicationFactory>
{
    private readonly TaxiPayWebApplicationFactory _factory;

    public TripLifecycleIntegrationTests(TaxiPayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task StartNewTrip_WhenNoActiveTrip_CreatesActiveTripInDatabase()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var route = TestBuilders.CreateRoute();
        var driver = TestBuilders.CreateDriver(driverId, "ET2001", routeId: route.Id);
        var shift = TestBuilders.CreateShift(driver.Id, route.Id);

        await _factory.ExecuteDbContextAsync(async db =>
        {
            db.Routes.Add(route);
            db.Drivers.Add(driver);
            db.DriverShifts.Add(shift);
            await db.SaveChangesAsync();
        });

        var client = _factory.CreateDriverClient(driverId, "ET2001");
        var startRequest = new StartTripRequest("A→C");

        // Act
        var response = await client.PostAsJsonAsync($"/api/drivers/shifts/{shift.Id}/trips/start", startRequest);

        // Assert - HTTP response
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tripDto = await response.Content.ReadFromJsonAsync<TripResponse>();
        Assert.NotNull(tripDto);
        Assert.Equal("Active", tripDto.Status);

        // Assert - Real DB persistence
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var tripInDb = await db.Trips.FirstOrDefaultAsync(t => t.DriverShiftId == shift.Id);
            Assert.NotNull(tripInDb);
            Assert.Equal(TripStatus.Active, tripInDb.Status);
            Assert.Equal(driverId, tripInDb.DriverId);
        });
    }

    [Fact]
    public async Task StartNewTrip_WhenTripAlreadyActive_ReturnsConflict()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var route = TestBuilders.CreateRoute();
        var driver = TestBuilders.CreateDriver(driverId, "ET2002", routeId: route.Id);
        var shift = TestBuilders.CreateShift(driver.Id, route.Id);
        var existingTrip = TestBuilders.CreateTrip(shift.Id, driver.Id, route.Id);
        shift.CurrentTripId = existingTrip.Id;

        await _factory.ExecuteDbContextAsync(async db =>
        {
            db.Routes.Add(route);
            db.Drivers.Add(driver);
            db.DriverShifts.Add(shift);
            db.Trips.Add(existingTrip);
            await db.SaveChangesAsync();
        });

        var client = _factory.CreateDriverClient(driverId, "ET2002");
        var startRequest = new StartTripRequest("A→C");

        // Act
        var response = await client.PostAsJsonAsync($"/api/drivers/shifts/{shift.Id}/trips/start", startRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("trip_already_active", content);
    }

    [Fact]
    public async Task CompleteCurrentTrip_WhenNoPassengersOnBoard_MarksTripCompletedInDatabase()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var route = TestBuilders.CreateRoute();
        var driver = TestBuilders.CreateDriver(driverId, "ET2003", routeId: route.Id);
        var shift = TestBuilders.CreateShift(driver.Id, route.Id);
        var activeTrip = TestBuilders.CreateTrip(shift.Id, driver.Id, route.Id);
        shift.CurrentTripId = activeTrip.Id;

        await _factory.ExecuteDbContextAsync(async db =>
        {
            db.Routes.Add(route);
            db.Drivers.Add(driver);
            db.DriverShifts.Add(shift);
            db.Trips.Add(activeTrip);
            await db.SaveChangesAsync();
        });

        var client = _factory.CreateDriverClient(driverId, "ET2003");
        var completeRequest = new CompleteTripRequest { Latitude = 9.0100m, Longitude = 38.7400m };

        // Act
        var response = await client.PostAsJsonAsync($"/api/drivers/shifts/{shift.Id}/trips/complete", completeRequest);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var tripDto = await response.Content.ReadFromJsonAsync<TripResponse>();
        Assert.NotNull(tripDto);
        Assert.Equal("Completed", tripDto.Status);

        // Assert - DB verification
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var tripInDb = await db.Trips.FindAsync(activeTrip.Id);
            Assert.NotNull(tripInDb);
            Assert.Equal(TripStatus.Completed, tripInDb.Status);
            Assert.NotNull(tripInDb.TripEnd);

            var updatedShift = await db.DriverShifts.FindAsync(shift.Id);
            Assert.Null(updatedShift!.CurrentTripId);
        });
    }
}
