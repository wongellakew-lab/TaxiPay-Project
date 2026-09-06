using Microsoft.EntityFrameworkCore;
using Moq;
using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Application.DTOs.Passengers;
using TaxiPayApi.Application.Interfaces;
using TaxiPayApi.Application.Services;
using TaxiPayApi.Domain.Entities;
using TaxiPayApi.Infrastructure.Persistence.Context;
using TaxiPayApi.Tests.Fixtures;

namespace TaxiPayApi.Tests.BusinessRules;

public class WalletAndProfileBusinessRuleTests : IDisposable
{
    private readonly TaxiPayDbContext _dbContext;
    private readonly TestRealtimeNotifier _realtimeNotifier;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<INotificationService> _mockNotificationService;
    private readonly PassengerService _passengerService;
    private readonly DriverService _driverService;

    public WalletAndProfileBusinessRuleTests()
    {
        var options = new DbContextOptionsBuilder<TaxiPayDbContext>()
            .UseInMemoryDatabase($"WalletProfileRules_{Guid.NewGuid():N}")
            .ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _dbContext = new TaxiPayDbContext(options);
        _realtimeNotifier = new TestRealtimeNotifier();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockNotificationService = new Mock<INotificationService>();

        _passengerService = TestServiceFactory.CreatePassengerService(
            _dbContext,
            _realtimeNotifier,
            _mockCurrentUserService.Object);

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
    public async Task TopUpWallet_WhenValidAmount_IncreasesBalanceAndPersistsToDatabase()
    {
        // Arrange
        var passenger = TestBuilders.CreatePassenger(walletBalance: 150.00m);
        _dbContext.Passengers.Add(passenger);
        await _dbContext.SaveChangesAsync();

        var request = new TopUpWalletRequest { Amount = 350.00m, PaymentMethod = "Telebirr" };

        // Act
        var result = await _passengerService.TopUpWalletAsync(passenger.Id, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(500.00m, result.Value.NewBalance);
        Assert.Equal(350.00m, result.Value.Amount);

        // Verify Database State
        var freshPassenger = await _dbContext.Passengers.FindAsync(passenger.Id);
        Assert.Equal(500.00m, freshPassenger!.WalletBalance);
    }

    [Fact]
    public async Task UpdatePassengerProfile_WhenPhoneAlreadyInUse_ReturnsPhoneAlreadyInUseError()
    {
        // Arrange
        var passenger1 = TestBuilders.CreatePassenger(firstName: "User1", phone: "+251911111111");
        var passenger2 = TestBuilders.CreatePassenger(firstName: "User2", phone: "+251922222222");

        _dbContext.Passengers.AddRange(passenger1, passenger2);
        await _dbContext.SaveChangesAsync();

        var request = new UpdatePassengerProfileRequest
        {
            FirstName = "User2 Updated",
            LastName = "Name",
            Phone = "+251911111111" // Attempting to use passenger1's phone
        };

        // Act
        var result = await _passengerService.UpdateProfileAsync(passenger2.Id, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("phone_already_in_use", result.Error.Code);
    }

    [Fact]
    public async Task RequestRouteChange_WhenPendingRequestAlreadyExists_ReturnsDuplicateRequestError()
    {
        // Arrange
        var route1 = TestBuilders.CreateRoute(code: "RT-01");
        var route2 = TestBuilders.CreateRoute(code: "RT-02");
        var route3 = TestBuilders.CreateRoute(code: "RT-03");
        var driver = TestBuilders.CreateDriver(routeId: route1.Id);

        var existingRequest = new RouteChangeRequest
        {
            Id = Guid.NewGuid(),
            DriverId = driver.Id,
            CurrentRouteId = route1.Id,
            NewRouteId = route2.Id,
            Reason = "First relocation",
            Status = RouteChangeStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };

        _dbContext.Routes.AddRange(route1, route2, route3);
        _dbContext.Drivers.Add(driver);
        _dbContext.RouteChangeRequests.Add(existingRequest);
        await _dbContext.SaveChangesAsync();

        var newRequest = new RequestRouteChangeRequest
        {
            NewRouteId = route3.Id,
            Reason = "Second relocation attempt"
        };

        // Act
        var result = await _driverService.RequestRouteChangeAsync(driver.Id, newRequest);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("route_change_already_requested", result.Error.Code);
    }

    [Fact]
    public async Task RequestRouteChange_WhenRequestingSameRoute_ReturnsSameRouteRequestedError()
    {
        // Arrange
        var route1 = TestBuilders.CreateRoute(code: "RT-01");
        var driver = TestBuilders.CreateDriver(routeId: route1.Id);

        _dbContext.Routes.Add(route1);
        _dbContext.Drivers.Add(driver);
        await _dbContext.SaveChangesAsync();

        var request = new RequestRouteChangeRequest
        {
            NewRouteId = route1.Id, // Same route as currently assigned
            Reason = "Already here"
        };

        // Act
        var result = await _driverService.RequestRouteChangeAsync(driver.Id, request);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("same_route_requested", result.Error.Code);
    }
}
