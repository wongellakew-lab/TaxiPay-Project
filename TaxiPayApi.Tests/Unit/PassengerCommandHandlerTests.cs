using Moq;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Application.DTOs.Passengers;
using TaxiPayApi.Application.Features.Passengers.Commands.ConfirmAlighting;
using TaxiPayApi.Application.Features.Passengers.Commands.PayTrip;
using TaxiPayApi.Application.Features.Passengers.Commands.SubmitPassengerRouteRequest;
using TaxiPayApi.Application.Features.Passengers.Commands.TopUpWallet;
using TaxiPayApi.Application.Features.Passengers.Commands.UpdateProfile;
using TaxiPayApi.Application.Features.Passengers.Queries.GetTripHistory;
using TaxiPayApi.Application.Services;
using TaxiPayApi.Domain.Entities;

namespace TaxiPayApi.Tests.Unit;

public class PassengerCommandHandlerTests
{
    private readonly Mock<IPassengerService> _mockPassengerService;

    public PassengerCommandHandlerTests()
    {
        _mockPassengerService = new Mock<IPassengerService>();
    }

    [Fact]
    public async Task PayTripCommandHandler_WhenServiceSucceeds_ReturnsSuccessResult()
    {
        // Arrange
        var passengerId = Guid.NewGuid();
        var request = new PayTripRequest
        {
            DriverId = "ET-TX-7788",
            BoardingNode = "NodeA",
            DestinationNode = "NodeC",
            PaymentMethod = PaymentMethod.Telebirr
        };

        var expectedResponse = new PayTripResponse
        {
            TripId = Guid.NewGuid(),
            PassengerId = passengerId,
            DriverId = "ET-TX-7788",
            DriverName = "Abebe Bikila",
            RouteName = "Megenagna - Tor Hailoch",
            BoardingNode = "NodeA",
            DestinationNode = "NodeC",
            Fare = 25.00m,
            PaymentMethod = "Telebirr",
            PaymentStatus = "Completed",
            PaidAt = DateTime.UtcNow,
            ReceiptNumber = "RCP-2026-001",
            NewBalance = 475.00m
        };

        _mockPassengerService
            .Setup(s => s.PayForTripAsync(passengerId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PayTripResponse, PassengerError>.Success(expectedResponse));

        var handler = new PayTripCommandHandler(_mockPassengerService.Object);
        var command = new PayTripCommand(passengerId, request);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("RCP-2026-001", result.Value.ReceiptNumber);
        Assert.Equal(25.00m, result.Value.Fare);
        _mockPassengerService.Verify(s => s.PayForTripAsync(passengerId, request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PayTripCommandHandler_WhenInsufficientBalance_ReturnsFailureResult()
    {
        // Arrange
        var passengerId = Guid.NewGuid();
        var request = new PayTripRequest
        {
            DriverId = "ET-TX-7788",
            BoardingNode = "NodeA",
            DestinationNode = "NodeC",
            PaymentMethod = PaymentMethod.Wallet
        };

        var expectedError = PassengerError.InsufficientBalance();

        _mockPassengerService
            .Setup(s => s.PayForTripAsync(passengerId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PayTripResponse, PassengerError>.Failure(expectedError));

        var handler = new PayTripCommandHandler(_mockPassengerService.Object);
        var command = new PayTripCommand(passengerId, request);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("insufficient_balance", result.Error.Code);
    }

    [Fact]
    public async Task PayTripCommandHandler_WhenDriverHasNoActiveTrip_ReturnsFailureResult()
    {
        // Arrange
        var passengerId = Guid.NewGuid();
        var request = new PayTripRequest
        {
            DriverId = "ET-TX-7788",
            BoardingNode = "NodeA",
            DestinationNode = "NodeC",
            PaymentMethod = PaymentMethod.Telebirr
        };

        _mockPassengerService
            .Setup(s => s.PayForTripAsync(passengerId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PayTripResponse, PassengerError>.Failure(PassengerError.DriverNoActiveTrip()));

        var handler = new PayTripCommandHandler(_mockPassengerService.Object);
        var command = new PayTripCommand(passengerId, request);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("driver_no_active_trip", result.Error.Code);
    }

    [Fact]
    public async Task TopUpWalletCommandHandler_WhenSuccessful_ReturnsNewBalance()
    {
        // Arrange
        var passengerId = Guid.NewGuid();
        var request = new TopUpWalletRequest { Amount = 200.00m, PaymentMethod = "Telebirr" };
        var expectedResponse = new TopUpWalletResponse
        {
            PassengerId = passengerId,
            Amount = 200.00m,
            NewBalance = 700.00m,
            TransactionRef = "TXN-TOPUP-001",
            TopUpTime = DateTime.UtcNow
        };

        _mockPassengerService
            .Setup(s => s.TopUpWalletAsync(passengerId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TopUpWalletResponse, PassengerError>.Success(expectedResponse));

        var handler = new TopUpWalletCommandHandler(_mockPassengerService.Object);
        var command = new TopUpWalletCommand(passengerId, request);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(700.00m, result.Value.NewBalance);
        _mockPassengerService.Verify(s => s.TopUpWalletAsync(passengerId, request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdatePassengerProfileCommandHandler_WhenPhoneAlreadyRegistered_ReturnsPhoneInUseError()
    {
        // Arrange
        var passengerId = Guid.NewGuid();
        var request = new UpdatePassengerProfileRequest
        {
            FirstName = "Almaz",
            LastName = "Tesfaye",
            Phone = "+251911999999"
        };

        _mockPassengerService
            .Setup(s => s.UpdateProfileAsync(passengerId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<PassengerResponse, PassengerError>.Failure(PassengerError.PhoneAlreadyInUse()));

        var handler = new UpdatePassengerProfileCommandHandler(_mockPassengerService.Object);
        var command = new UpdatePassengerProfileCommand(passengerId, request);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("phone_already_in_use", result.Error.Code);
    }

    [Fact]
    public async Task ConfirmAlightingCommandHandler_WhenSuccessful_ReturnsAlightedStatus()
    {
        // Arrange
        var passengerId = Guid.NewGuid();
        var journeyId = Guid.NewGuid();
        var expectedDto = new ActivePassengerDto
        {
            JourneyId = journeyId,
            PassengerId = passengerId,
            PassengerName = "Almaz Tesfaye",
            PassengerPhone = "+251911000000",
            BoardingNode = "NodeA",
            DestinationNode = "NodeC",
            Fare = 25.00m,
            PaymentMethod = "Telebirr",
            PaymentStatus = "Completed",
            AlightingStatus = "Alighted",
            BoardedAt = DateTime.UtcNow.AddMinutes(-20)
        };

        _mockPassengerService
            .Setup(s => s.ConfirmPassengerAlightingAsync(passengerId, journeyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ActivePassengerDto, PassengerError>.Success(expectedDto));

        var handler = new ConfirmAlightingCommandHandler(_mockPassengerService.Object);
        var command = new ConfirmAlightingCommand(passengerId, journeyId);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("Alighted", result.Value.AlightingStatus);
    }

    [Fact]
    public async Task GetPassengerTripHistoryQueryHandler_ReturnsPaginatedResult()
    {
        // Arrange
        var passengerId = Guid.NewGuid();
        var expectedHistory = new TripHistoryResponse
        {
            Trips = new List<TripHistoryItemDto>(),
            TotalCount = 0,
            PageNumber = 1,
            PageSize = 10,
            TotalPages = 0,
            HasNextPage = false,
            HasPreviousPage = false
        };

        _mockPassengerService
            .Setup(s => s.GetTripHistoryAsync(passengerId, 1, 10, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TripHistoryResponse, PassengerError>.Success(expectedHistory));

        var handler = new GetPassengerTripHistoryQueryHandler(_mockPassengerService.Object);
        var query = new GetPassengerTripHistoryQuery(passengerId, 1, 10);

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value.PageNumber);
        Assert.Equal(10, result.Value.PageSize);
    }
}
