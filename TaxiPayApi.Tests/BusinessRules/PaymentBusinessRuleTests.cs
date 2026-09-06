using Microsoft.EntityFrameworkCore;
using Moq;
using TaxiPayApi.Application.DTOs.Passengers;
using TaxiPayApi.Application.Interfaces;
using TaxiPayApi.Application.Services;
using TaxiPayApi.Domain.Entities;
using TaxiPayApi.Infrastructure.Persistence.Context;
using TaxiPayApi.Tests.Fixtures;

namespace TaxiPayApi.Tests.BusinessRules;

public class PaymentBusinessRuleTests : IDisposable
{
    private readonly TaxiPayDbContext _dbContext;
    private readonly TestRealtimeNotifier _realtimeNotifier;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly PassengerService _passengerService;

    public PaymentBusinessRuleTests()
    {
        var options = new DbContextOptionsBuilder<TaxiPayDbContext>()
            .UseInMemoryDatabase($"PaymentRules_{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _dbContext = new TaxiPayDbContext(options);
        _realtimeNotifier = new TestRealtimeNotifier();
        _mockCurrentUserService = new Mock<ICurrentUserService>();

        _passengerService = TestServiceFactory.CreatePassengerService(
            _dbContext,
            _realtimeNotifier,
            _mockCurrentUserService.Object);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
    }

    [Fact]
    public async Task PayForTrip_WhenDriverHasNoActiveTrip_ReturnsDriverNoActiveTripError()
    {
        // Arrange
        var route = TestBuilders.CreateRoute();
        var fare = TestBuilders.CreateFare(route.Id, "A", "C", 25.00m);
        var driver = TestBuilders.CreateDriver(routeId: route.Id);
        var shift = TestBuilders.CreateShift(driver.Id, route.Id);
        var passenger = TestBuilders.CreatePassenger(walletBalance: 100.00m);

        // Intentionally no active trip in database
        _dbContext.Routes.Add(route);
        _dbContext.FareMatrices.Add(fare);
        _dbContext.Drivers.Add(driver);
        _dbContext.DriverShifts.Add(shift);
        _dbContext.Passengers.Add(passenger);
        await _dbContext.SaveChangesAsync();

        var request = new PayTripRequest
        {
            DriverId = driver.DriverId!,
            BoardingNode = "A",
            DestinationNode = "C",
            PaymentMethod = PaymentMethod.Wallet
        };

        // Act
        var result = await _passengerService.PayForTripAsync(passenger.Id, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("driver_no_active_trip", result.Error.Code);
    }

    [Fact]
    public async Task PayForTrip_WhenTripIsCompleted_ReturnsTripNotActiveError()
    {
        // Arrange
        var route = TestBuilders.CreateRoute();
        var fare = TestBuilders.CreateFare(route.Id, "A", "C", 25.00m);
        var driver = TestBuilders.CreateDriver(routeId: route.Id);
        var shift = TestBuilders.CreateShift(driver.Id, route.Id);
        var completedTrip = TestBuilders.CreateTrip(shift.Id, driver.Id, route.Id, status: TripStatus.Completed);
        var passenger = TestBuilders.CreatePassenger(walletBalance: 100.00m);

        _dbContext.Routes.Add(route);
        _dbContext.FareMatrices.Add(fare);
        _dbContext.Drivers.Add(driver);
        _dbContext.DriverShifts.Add(shift);
        _dbContext.Trips.Add(completedTrip);
        _dbContext.Passengers.Add(passenger);
        await _dbContext.SaveChangesAsync();

        var request = new PayTripRequest
        {
            DriverId = driver.DriverId!,
            BoardingNode = "A",
            DestinationNode = "C",
            PaymentMethod = PaymentMethod.Wallet
        };

        // Act
        var result = await _passengerService.PayForTripAsync(passenger.Id, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("driver_no_active_trip", result.Error.Code);
    }

    [Fact]
    public async Task PayForTrip_WhenInsufficientWalletBalance_ReturnsInsufficientBalanceError()
    {
        // Arrange
        var route = TestBuilders.CreateRoute();
        var fare = TestBuilders.CreateFare(route.Id, "A", "C", 50.00m);
        var driver = TestBuilders.CreateDriver(routeId: route.Id);
        var shift = TestBuilders.CreateShift(driver.Id, route.Id);
        var trip = TestBuilders.CreateTrip(shift.Id, driver.Id, route.Id, status: TripStatus.Active);
        shift.CurrentTripId = trip.Id;

        // Passenger wallet has only 20 ETB, but fare is 50 ETB
        var passenger = TestBuilders.CreatePassenger(walletBalance: 20.00m);

        _dbContext.Routes.Add(route);
        _dbContext.FareMatrices.Add(fare);
        _dbContext.Drivers.Add(driver);
        _dbContext.DriverShifts.Add(shift);
        _dbContext.Trips.Add(trip);
        _dbContext.Passengers.Add(passenger);
        await _dbContext.SaveChangesAsync();

        var request = new PayTripRequest
        {
            DriverId = driver.DriverId!,
            BoardingNode = "A",
            DestinationNode = "C",
            PaymentMethod = PaymentMethod.Wallet
        };

        // Act
        var result = await _passengerService.PayForTripAsync(passenger.Id, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("insufficient_balance", result.Error.Code);

        // Verify balance was untouched
        var freshPassenger = await _dbContext.Passengers.FindAsync(passenger.Id);
        Assert.Equal(20.00m, freshPassenger!.WalletBalance);
    }

    [Fact]
    public async Task PayForTrip_WhenSuccessful_UpdatesWallet_TripRevenue_Receipt_AndManifest()
    {
        // Arrange
        var route = TestBuilders.CreateRoute();
        var fare = TestBuilders.CreateFare(route.Id, "A", "C", 30.00m);
        var driver = TestBuilders.CreateDriver(routeId: route.Id);
        var shift = TestBuilders.CreateShift(driver.Id, route.Id);
        var trip = TestBuilders.CreateTrip(shift.Id, driver.Id, route.Id, status: TripStatus.Active);
        shift.CurrentTripId = trip.Id;
        var passenger = TestBuilders.CreatePassenger(walletBalance: 100.00m);

        _dbContext.Routes.Add(route);
        _dbContext.FareMatrices.Add(fare);
        _dbContext.Drivers.Add(driver);
        _dbContext.DriverShifts.Add(shift);
        _dbContext.Trips.Add(trip);
        _dbContext.Passengers.Add(passenger);
        await _dbContext.SaveChangesAsync();

        var request = new PayTripRequest
        {
            DriverId = driver.DriverId!,
            BoardingNode = "A",
            DestinationNode = "C",
            PaymentMethod = PaymentMethod.Wallet
        };

        // Act
        var result = await _passengerService.PayForTripAsync(passenger.Id, request);

        // Assert HTTP / Service Result
        Assert.True(result.IsSuccess);
        Assert.Equal(70.00m, result.Value.NewBalance);
        Assert.Equal(30.00m, result.Value.Fare);
        Assert.StartsWith("RCP-", result.Value.ReceiptNumber);

        // Assert Database State: Passenger Wallet
        var freshPassenger = await _dbContext.Passengers.FindAsync(passenger.Id);
        Assert.Equal(70.00m, freshPassenger!.WalletBalance);
        Assert.Equal(30.00m, freshPassenger.TotalSpent);
        Assert.Equal(1, freshPassenger.TotalTrips);

        // Assert Database State: Trip & Shift Revenue
        var freshTrip = await _dbContext.Trips.FindAsync(trip.Id);
        Assert.Equal(30.00m, freshTrip!.TripRevenue);
        Assert.Equal(1, freshTrip.PassengersCount);

        var freshShift = await _dbContext.DriverShifts.FindAsync(shift.Id);
        Assert.Equal(30.00m, freshShift!.TotalRevenue);
        Assert.Equal(1, freshShift.TotalPassengers);

        // Assert Database State: Journey & Receipt
        var journey = await _dbContext.PassengerJourneyss.FirstOrDefaultAsync(j => j.TripId == trip.Id);
        Assert.NotNull(journey);
        Assert.Equal(PaymentStatus.Completed, journey.PaymentStatus);
        Assert.Equal(AlightingStatus.OnBoard, journey.AlightingStatus);
        Assert.Equal(30.00m, journey.Fare);

        var receipt = await _dbContext.Receipts.FirstOrDefaultAsync(r => r.JourneyId == journey.Id);
        Assert.NotNull(receipt);
        Assert.Equal(result.Value.ReceiptNumber, receipt.ReceiptNumber);

        // Assert SignalR Event: Driver Manifest Update
        Assert.Single(_realtimeNotifier.ManifestAdditions);
        var manifestEntry = _realtimeNotifier.ManifestAdditions.First();
        Assert.Equal(passenger.Id, manifestEntry.PassengerId);
        Assert.Equal(30.00m, manifestEntry.Fare);
    }

    [Fact]
    public async Task PayForTrip_WhenPassengerDoesNotOwnAccount_ReturnsForbidden()
    {
        // Arrange
        var authenticatedPassengerId = Guid.NewGuid();
        var targetPassengerId = Guid.NewGuid();

        _mockCurrentUserService.Setup(u => u.IsAuthenticated).Returns(true);
        _mockCurrentUserService.Setup(u => u.IsAdmin).Returns(false);
        _mockCurrentUserService.Setup(u => u.PassengerGuid).Returns(authenticatedPassengerId);

        var request = new PayTripRequest
        {
            DriverId = "ET-TX-7788",
            BoardingNode = "A",
            DestinationNode = "C",
            PaymentMethod = PaymentMethod.Wallet
        };

        // Act: Attempting to pay with targetPassengerId while authenticated as authenticatedPassengerId
        var result = await _passengerService.PayForTripAsync(targetPassengerId, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("forbidden", result.Error.Code);
    }
}
