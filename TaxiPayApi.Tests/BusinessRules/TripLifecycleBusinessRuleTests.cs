using Microsoft.EntityFrameworkCore;
using Moq;
using TaxiPayApi.Application.Interfaces;
using TaxiPayApi.Application.Services;
using TaxiPayApi.Domain.Entities;
using TaxiPayApi.Infrastructure.Persistence.Context;
using TaxiPayApi.Tests.Fixtures;

namespace TaxiPayApi.Tests.BusinessRules;

public class TripLifecycleBusinessRuleTests : IDisposable
{
    private readonly TaxiPayDbContext _dbContext;
    private readonly TestRealtimeNotifier _realtimeNotifier;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly DriverService _driverService;

    public TripLifecycleBusinessRuleTests()
    {
        var options = new DbContextOptionsBuilder<TaxiPayDbContext>()
            .UseInMemoryDatabase($"TripLifecycleRules_{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _dbContext = new TaxiPayDbContext(options);
        _realtimeNotifier = new TestRealtimeNotifier();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockNotificationService = new Mock<INotificationService>();

        _driverService = TestServiceFactory.CreateDriverService(
            _dbContext,
            _realtimeNotifier,
            _mockCurrentUserService.Object,
            _mockNotificationService.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task StartNewTrip_WhenDriverAlreadyHasActiveTrip_ReturnsTripAlreadyActiveError()
    {
        // Arrange
        var route = TestBuilders.CreateRoute();
        var driver = TestBuilders.CreateDriver(routeId: route.Id);
        var shift = TestBuilders.CreateShift(driver.Id, route.Id);
        var activeTrip = TestBuilders.CreateTrip(shift.Id, driver.Id, route.Id, status: TripStatus.Active);
        shift.CurrentTripId = activeTrip.Id;

        _dbContext.Routes.Add(route);
        _dbContext.Drivers.Add(driver);
        _dbContext.DriverShifts.Add(shift);
        _dbContext.Trips.Add(activeTrip);
        await _dbContext.SaveChangesAsync();

        // Act: Attempt to start a second trip while first is still active
        var result = await _driverService.StartNewTripAsync(shift.Id, "A→C");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("trip_already_active", result.Error.Code);
    }

    [Fact]
    public async Task StartNewTrip_WhenDirectionContainsInvalidNodes_ReturnsInvalidDirectionError()
    {
        // Arrange
        var route = TestBuilders.CreateRoute();
        var driver = TestBuilders.CreateDriver(routeId: route.Id);
        var shift = TestBuilders.CreateShift(driver.Id, route.Id);

        _dbContext.Routes.Add(route);
        _dbContext.Drivers.Add(driver);
        _dbContext.DriverShifts.Add(shift);
        await _dbContext.SaveChangesAsync();

        // Act: Direction with non-existent nodes "UnknownX→UnknownY"
        var result = await _driverService.StartNewTripAsync(shift.Id, "UnknownX→UnknownY");

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("invalid_direction", result.Error.Code);
    }

    [Fact]
    public async Task CompleteCurrentTrip_WhenPassengersStillOnBoard_ReturnsCannotCompleteTripError()
    {
        // Arrange
        var route = TestBuilders.CreateRoute();
        var driver = TestBuilders.CreateDriver(routeId: route.Id);
        var shift = TestBuilders.CreateShift(driver.Id, route.Id);
        var trip = TestBuilders.CreateTrip(shift.Id, driver.Id, route.Id, status: TripStatus.Active, passengersCount: 2);
        shift.CurrentTripId = trip.Id;

        var passenger = TestBuilders.CreatePassenger();
        var journey1 = TestBuilders.CreateJourney(trip.Id, passenger.Id, alightingStatus: AlightingStatus.OnBoard);
        var journey2 = TestBuilders.CreateJourney(trip.Id, passenger.Id, alightingStatus: AlightingStatus.AwaitingConfirmation);

        _dbContext.Routes.Add(route);
        _dbContext.Drivers.Add(driver);
        _dbContext.DriverShifts.Add(shift);
        _dbContext.Trips.Add(trip);
        _dbContext.Passengers.Add(passenger);
        _dbContext.PassengerJourneyss.AddRange(journey1, journey2);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _driverService.CompleteCurrentTripAsync(shift.Id, 9.0100m, 38.7400m);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("cannot_complete_trip", result.Error.Code);
        Assert.Contains("2 passenger(s)", result.Error.Message);

        // Verify trip remains Active
        var freshTrip = await _dbContext.Trips.FindAsync(trip.Id);
        Assert.Equal(TripStatus.Active, freshTrip!.Status);
    }

    [Fact]
    public async Task CompleteCurrentTrip_WhenAllPassengersAlighted_SucceedsAndUpdatesDatabase()
    {
        // Arrange
        var route = TestBuilders.CreateRoute();
        var driver = TestBuilders.CreateDriver(routeId: route.Id);
        var shift = TestBuilders.CreateShift(driver.Id, route.Id);
        var trip = TestBuilders.CreateTrip(shift.Id, driver.Id, route.Id, status: TripStatus.Active, passengersCount: 1);
        trip.TripRevenue = 30.00m;
        shift.CurrentTripId = trip.Id;

        var passenger = TestBuilders.CreatePassenger();
        var journey = TestBuilders.CreateJourney(trip.Id, passenger.Id, alightingStatus: AlightingStatus.Alighted);

        _dbContext.Routes.Add(route);
        _dbContext.Drivers.Add(driver);
        _dbContext.DriverShifts.Add(shift);
        _dbContext.Trips.Add(trip);
        _dbContext.Passengers.Add(passenger);
        _dbContext.PassengerJourneyss.Add(journey);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _driverService.CompleteCurrentTripAsync(shift.Id, 9.0100m, 38.7400m);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Completed", result.Value.Status);

        // Verify Trip updated in DB
        var freshTrip = await _dbContext.Trips.FindAsync(trip.Id);
        Assert.Equal(TripStatus.Completed, freshTrip!.Status);
        Assert.NotNull(freshTrip.TripEnd);
        Assert.True(freshTrip.HasReachedFinalDestination);

        // Verify Shift updated in DB
        var freshShift = await _dbContext.DriverShifts.FindAsync(shift.Id);
        Assert.Null(freshShift!.CurrentTripId);
        Assert.Equal(1, freshShift.TotalPassengers);
        Assert.Equal(30.00m, freshShift.TotalRevenue);
    }

    [Fact]
    public async Task CompleteCurrentTrip_WhenDriverDoesNotOwnShift_ReturnsForbidden()
    {
        // Arrange
        var shiftOwnerDriverId = Guid.NewGuid();
        var callerDriverId = Guid.NewGuid();

        _mockCurrentUserService.Setup(u => u.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(u => u.IsAdmin).Returns(false);
        _mockCurrentUserService.Setup(u => u.DriverGuid).Returns(callerDriverId);

        var route = TestBuilders.CreateRoute();
        var driver = TestBuilders.CreateDriver(shiftOwnerDriverId);
        var shift = TestBuilders.CreateShift(shiftOwnerDriverId, route.Id);

        _dbContext.Routes.Add(route);
        _dbContext.Drivers.Add(driver);
        _dbContext.DriverShifts.Add(shift);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _driverService.CompleteCurrentTripAsync(shift.Id, 9.0100m, 38.7400m);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("forbidden", result.Error.Code);
    }
}
