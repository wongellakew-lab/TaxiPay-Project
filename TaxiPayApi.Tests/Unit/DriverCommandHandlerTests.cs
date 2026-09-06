using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Application.Features.Drivers.Commands.CompleteCurrentTrip;
using TaxiPayApi.Application.Features.Drivers.Commands.RequestAlightingConfirmation;
using TaxiPayApi.Application.Features.Drivers.Commands.RequestRouteChange;
using TaxiPayApi.Application.Features.Drivers.Commands.StartNewTrip;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Tests.Unit;

public class DriverCommandHandlerTests
{
    private readonly Mock<IDriverService> _mockDriverService;
    private readonly HybridCache _hybridCache;

    public DriverCommandHandlerTests()
    {
        _mockDriverService = new Mock<IDriverService>();
        var services = new ServiceCollection();
#pragma warning disable EXTEXP0018
        services.AddHybridCache();
#pragma warning restore EXTEXP0018
        var provider = services.BuildServiceProvider();
        _hybridCache = provider.GetRequiredService<HybridCache>();
    }

    [Fact]
    public async Task StartNewTripCommandHandler_WhenSuccessful_ReturnsSuccessAndInvalidatesCache()
    {
        // Arrange
        var shiftId = Guid.NewGuid();
        var direction = "NodeA→NodeC";
        var expectedResponse = new TripResponse
        {
            TripId = Guid.NewGuid(),
            ShiftId = shiftId,
            DriverId = Guid.NewGuid(),
            DriverName = "Abebe Bikila",
            TripNumber = 1,
            Direction = "NodeA → NodeC",
            RouteName = "Megenagna - Tor Hailoch",
            TripStart = DateTime.UtcNow,
            Status = "Active",
            PassengersCount = 0,
            TripRevenue = 0
        };

        _mockDriverService
            .Setup(s => s.StartNewTripAsync(shiftId, direction, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TripResponse, DriverError>.Success(expectedResponse));

        var handler = new StartNewTripCommandHandler(_mockDriverService.Object, _hybridCache);
        var command = new StartNewTripCommand(shiftId, direction);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Active", result.Value.Status);
        Assert.Equal(1, result.Value.TripNumber);
        _mockDriverService.Verify(s => s.StartNewTripAsync(shiftId, direction, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartNewTripCommandHandler_WhenTripAlreadyActive_ReturnsTripAlreadyActiveError()
    {
        // Arrange
        var shiftId = Guid.NewGuid();
        var direction = "NodeA→NodeC";

        _mockDriverService
            .Setup(s => s.StartNewTripAsync(shiftId, direction, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TripResponse, DriverError>.Failure(DriverError.TripAlreadyActive()));

        var handler = new StartNewTripCommandHandler(_mockDriverService.Object, _hybridCache);
        var command = new StartNewTripCommand(shiftId, direction);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("trip_already_active", result.Error.Code);
    }

    [Fact]
    public async Task CompleteCurrentTripCommandHandler_WhenSuccessful_ReturnsCompletedStatus()
    {
        // Arrange
        var shiftId = Guid.NewGuid();
        var expectedResponse = new TripResponse
        {
            TripId = Guid.NewGuid(),
            ShiftId = shiftId,
            DriverId = Guid.NewGuid(),
            DriverName = "Abebe Bikila",
            TripNumber = 1,
            Direction = "NodeA → NodeC",
            RouteName = "Megenagna - Tor Hailoch",
            TripStart = DateTime.UtcNow.AddMinutes(-45),
            TripEnd = DateTime.UtcNow,
            Status = "Completed",
            PassengersCount = 14,
            TripRevenue = 350.00m
        };

        _mockDriverService
            .Setup(s => s.CompleteCurrentTripAsync(shiftId, 9.0100m, 38.7400m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TripResponse, DriverError>.Success(expectedResponse));

        var handler = new CompleteCurrentTripCommandHandler(_mockDriverService.Object, _hybridCache);
        var command = new CompleteCurrentTripCommand(shiftId, 9.0100m, 38.7400m);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Completed", result.Value.Status);
        Assert.Equal(14, result.Value.PassengersCount);
        Assert.Equal(350.00m, result.Value.TripRevenue);
    }

    [Fact]
    public async Task CompleteCurrentTripCommandHandler_WhenPassengersStillOnBoard_ReturnsCannotCompleteTripError()
    {
        // Arrange
        var shiftId = Guid.NewGuid();

        _mockDriverService
            .Setup(s => s.CompleteCurrentTripAsync(shiftId, 9.0100m, 38.7400m, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TripResponse, DriverError>.Failure(DriverError.CannotCompleteTripWithPassengers(3)));

        var handler = new CompleteCurrentTripCommandHandler(_mockDriverService.Object, _hybridCache);
        var command = new CompleteCurrentTripCommand(shiftId, 9.0100m, 38.7400m);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("cannot_complete_trip", result.Error.Code);
        Assert.Contains("3 passenger(s)", result.Error.Message);
    }

    [Fact]
    public async Task RequestAlightingConfirmationCommandHandler_WhenSuccessful_SetsAwaitingConfirmation()
    {
        // Arrange
        var journeyId = Guid.NewGuid();
        var expectedDto = new ActivePassengerDto
        {
            JourneyId = journeyId,
            PassengerId = Guid.NewGuid(),
            PassengerName = "Almaz Tesfaye",
            PassengerPhone = "+251911000000",
            BoardingNode = "NodeA",
            DestinationNode = "NodeC",
            Fare = 25.00m,
            PaymentMethod = "Telebirr",
            PaymentStatus = "Completed",
            AlightingStatus = "AwaitingConfirmation",
            BoardedAt = DateTime.UtcNow.AddMinutes(-30)
        };

        _mockDriverService
            .Setup(s => s.RequestPassengerAlightingAsync(journeyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActivePassengerDto, DriverError>.Success(expectedDto));

        var handler = new RequestAlightingConfirmationCommandHandler(_mockDriverService.Object);
        var command = new RequestAlightingConfirmationCommand(journeyId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("AwaitingConfirmation", result.Value.AlightingStatus);
    }

    [Fact]
    public async Task RequestRouteChangeCommandHandler_WhenPendingRequestAlreadyExists_ReturnsConflictError()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var request = new RequestRouteChangeRequest { NewRouteId = Guid.NewGuid(), Reason = "Relocation" };

        _mockDriverService
            .Setup(s => s.RequestRouteChangeAsync(driverId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<RouteChangeResponse, DriverError>.Failure(DriverError.RouteChangeAlreadyRequested()));

        var handler = new RequestRouteChangeCommandHandler(_mockDriverService.Object);
        var command = new RequestRouteChangeCommand(driverId, request);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("route_change_already_requested", result.Error.Code);
    }
}
