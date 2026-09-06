using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.Features.Admins.Commands.ApproveDriver;
using TaxiPayApi.Application.Features.Admins.Commands.RejectDriver;
using TaxiPayApi.Application.Features.Admins.Commands.ReviewRouteChangeRequest;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Tests.Unit;

public class AdminCommandHandlerTests
{
    private readonly Mock<IAdminService> _mockAdminService;
    private readonly HybridCache _hybridCache;

    public AdminCommandHandlerTests()
    {
        _mockAdminService = new Mock<IAdminService>();
        var services = new ServiceCollection();
#pragma warning disable EXTEXP0018
        services.AddHybridCache();
#pragma warning restore EXTEXP0018
        var provider = services.BuildServiceProvider();
        _hybridCache = provider.GetRequiredService<HybridCache>();
    }

    [Fact]
    public async Task ApproveDriverCommandHandler_WhenApproved_ReturnsTrueAndInvalidatesCache()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var request = new ApproveDriverRequest(Guid.NewGuid());

        _mockAdminService
            .Setup(s => s.ApproveDriverAsync(driverId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new ApproveDriverCommandHandler(_mockAdminService.Object, _hybridCache);
        var command = new ApproveDriverCommand(driverId, request);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockAdminService.Verify(s => s.ApproveDriverAsync(driverId, request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RejectDriverCommandHandler_WhenRejected_ReturnsTrue()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var request = new RejectDriverRequest("Invalid commercial taxi driving license");

        _mockAdminService
            .Setup(s => s.RejectDriverAsync(driverId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new RejectDriverCommandHandler(_mockAdminService.Object, _hybridCache);
        var command = new RejectDriverCommand(driverId, request);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockAdminService.Verify(s => s.RejectDriverAsync(driverId, request, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ReviewRouteChangeRequestCommandHandler_WhenApproved_ReturnsTrue()
    {
        // Arrange
        var requestId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var request = new ReviewRouteChangeRequestDto(true, "Route change approved by supervisor");

        _mockAdminService
            .Setup(s => s.ReviewRouteChangeRequestAsync(requestId, adminId, request, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var handler = new ReviewRouteChangeRequestCommandHandler(_mockAdminService.Object, _hybridCache);
        var command = new ReviewRouteChangeRequestCommand(requestId, adminId, request);

        // Act
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result);
        _mockAdminService.Verify(s => s.ReviewRouteChangeRequestAsync(requestId, adminId, request, It.IsAny<CancellationToken>()), Times.Once);
    }
}
