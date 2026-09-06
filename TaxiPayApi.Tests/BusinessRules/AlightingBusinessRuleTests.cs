using Microsoft.EntityFrameworkCore;
using Moq;
using TaxiPayApi.Application.Interfaces;
using TaxiPayApi.Application.Services;
using TaxiPayApi.Domain.Entities;
using TaxiPayApi.Infrastructure.Persistence.Context;
using TaxiPayApi.Tests.Fixtures;

namespace TaxiPayApi.Tests.BusinessRules;

public class AlightingBusinessRuleTests : IDisposable
{
    private readonly TaxiPayDbContext _dbContext;
    private readonly TestRealtimeNotifier _realtimeNotifier;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly DriverService _driverService;
    private readonly PassengerService _passengerService;

    public AlightingBusinessRuleTests()
    {
        var options = new DbContextOptionsBuilder<TaxiPayDbContext>()
            .UseInMemoryDatabase($"AlightingRules_{Guid.NewGuid():N}")
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

        _passengerService = TestServiceFactory.CreatePassengerService(
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
    public async Task RequestPassengerAlighting_ByDriver_SetsStatusToAwaitingConfirmation_AndNotifiesPassenger()
    {
        // Arrange
        var route = TestBuilders.CreateRoute();
        var driver = TestBuilders.CreateDriver(routeId: route.Id);
        var shift = TestBuilders.CreateShift(driver.Id, route.Id);
        var trip = TestBuilders.CreateTrip(shift.Id, driver.Id, route.Id);
        var passenger = TestBuilders.CreatePassenger();
        var journey = TestBuilders.CreateJourney(trip.Id, passenger.Id, alightingStatus: AlightingStatus.OnBoard);

        _dbContext.Routes.Add(route);
        _dbContext.Drivers.Add(driver);
        _dbContext.DriverShifts.Add(shift);
        _dbContext.Trips.Add(trip);
        _dbContext.Passengers.Add(passenger);
        _dbContext.PassengerJourneyss.Add(journey);
        await _dbContext.SaveChangesAsync();

        // Act: Driver marks passenger as alighted
        var result = await _driverService.RequestPassengerAlightingAsync(journey.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("AwaitingConfirmation", result.Value.AlightingStatus);

        // Verify Database State
        var freshJourney = await _dbContext.PassengerJourneyss.FindAsync(journey.Id);
        Assert.Equal(AlightingStatus.AwaitingConfirmation, freshJourney!.AlightingStatus);

        // Verify Realtime Notification
        Assert.Single(_realtimeNotifier.AlightingRequests);
        Assert.Equal(passenger.Id, _realtimeNotifier.AlightingRequests.First().PassengerId);

        // Verify Persistent Notification created for Passenger
        _mockNotificationService.Verify(n => n.CreateNotificationAsync(
            passenger.Id,
            "Passenger",
            "Alighting Confirmation Requested",
            It.IsAny<string>(),
            "warning",
            "/passenger/dashboard",
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConfirmPassengerAlighting_ByPassenger_SetsStatusToAlighted_AndRecordsTimestamp()
    {
        // Arrange
        var route = TestBuilders.CreateRoute();
        var driver = TestBuilders.CreateDriver(routeId: route.Id);
        var shift = TestBuilders.CreateShift(driver.Id, route.Id);
        var trip = TestBuilders.CreateTrip(shift.Id, driver.Id, route.Id);
        var passenger = TestBuilders.CreatePassenger();
        var journey = TestBuilders.CreateJourney(trip.Id, passenger.Id, alightingStatus: AlightingStatus.AwaitingConfirmation);

        _dbContext.Routes.Add(route);
        _dbContext.Drivers.Add(driver);
        _dbContext.DriverShifts.Add(shift);
        _dbContext.Trips.Add(trip);
        _dbContext.Passengers.Add(passenger);
        _dbContext.PassengerJourneyss.Add(journey);
        await _dbContext.SaveChangesAsync();

        // Act: Passenger confirms alighting
        var result = await _passengerService.ConfirmPassengerAlightingAsync(passenger.Id, journey.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Alighted", result.Value.AlightingStatus);

        // Verify Database State
        var freshJourney = await _dbContext.PassengerJourneyss.FindAsync(journey.Id);
        Assert.Equal(AlightingStatus.Alighted, freshJourney!.AlightingStatus);
        Assert.NotNull(freshJourney.AlightedAt);

        // Verify Realtime Notification to driver
        Assert.Single(_realtimeNotifier.AlightingConfirmations);
    }

    [Fact]
    public async Task ConfirmPassengerAlighting_WhenCalledByDifferentPassenger_ReturnsUnauthorized()
    {
        // Arrange
        var route = TestBuilders.CreateRoute();
        var driver = TestBuilders.CreateDriver(routeId: route.Id);
        var shift = TestBuilders.CreateShift(driver.Id, route.Id);
        var trip = TestBuilders.CreateTrip(shift.Id, driver.Id, route.Id);
        var journeyOwner = TestBuilders.CreatePassenger();
        var imposter = TestBuilders.CreatePassenger();
        var journey = TestBuilders.CreateJourney(trip.Id, journeyOwner.Id, alightingStatus: AlightingStatus.AwaitingConfirmation);

        _dbContext.Routes.Add(route);
        _dbContext.Drivers.Add(driver);
        _dbContext.DriverShifts.Add(shift);
        _dbContext.Trips.Add(trip);
        _dbContext.Passengers.AddRange(journeyOwner, imposter);
        _dbContext.PassengerJourneyss.Add(journey);
        await _dbContext.SaveChangesAsync();

        // Act: Imposter attempts to confirm journeyOwner's alighting
        var result = await _passengerService.ConfirmPassengerAlightingAsync(imposter.Id, journey.Id);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("unauthorized_passenger", result.Error.Code);
    }
}
