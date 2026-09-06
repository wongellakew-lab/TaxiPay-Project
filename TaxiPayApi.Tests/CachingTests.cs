using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using TaxiPayApi.Application.Common;
using TaxiPayApi.Application.Common.Configuration;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.DTOs.Passengers;
using TaxiPayApi.Application.Features.Admins.Commands.CreateRoute;
using TaxiPayApi.Application.Features.Admins.Commands.UpdateRoute;
using TaxiPayApi.Application.Features.Routes.Queries.GetActiveRoutes;
using TaxiPayApi.Application.Features.Routes.Queries.GetRouteById;
using TaxiPayApi.Application.Interfaces;
using TaxiPayApi.Application.Services;

namespace TaxiPayApi.Tests;

public class CachingTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly HybridCache _hybridCache;
    private readonly IOptions<CacheSettings> _cacheSettings;

    public CachingTests()
    {
        var services = new ServiceCollection();
#pragma warning disable EXTEXP0018
        services.AddHybridCache();
#pragma warning restore EXTEXP0018
        _serviceProvider = services.BuildServiceProvider();
        _hybridCache = _serviceProvider.GetRequiredService<HybridCache>();
        _cacheSettings = Options.Create(new CacheSettings
        {
            DefaultExpirationMinutes = 5,
            RouteExpirationMinutes = 10,
            FareMatrixExpirationMinutes = 10,
            DriverLookupExpirationMinutes = 5
        });
    }

    [Fact]
    public async Task GetActiveRoutes_CacheMiss_ThenCacheHit_CallsServiceOnlyOnce()
    {
        // Arrange
        var mockRouteService = new Mock<IRouteService>();
        var sampleRoutes = new List<RouteDetailDto>
        {
            new(Guid.NewGuid(), "Bole - Megenagna", "BOL-MEG", [], true, 5, DateTime.UtcNow)
        };

        mockRouteService
            .Setup(s => s.GetActiveRoutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sampleRoutes);

        var handler = new GetActiveRoutesQueryHandler(mockRouteService.Object, _hybridCache, _cacheSettings);

        // Act 1: Initial call (Cache Miss)
        var result1 = await handler.Handle(new GetActiveRoutesQuery(), CancellationToken.None);

        // Act 2: Second call (Cache Hit)
        var result2 = await handler.Handle(new GetActiveRoutesQuery(), CancellationToken.None);

        // Assert
        Assert.NotNull(result1);
        Assert.NotNull(result2);
        Assert.Single(result1);
        Assert.Single(result2);
        Assert.Equal("BOL-MEG", result1[0].RouteCode);
        Assert.Equal("BOL-MEG", result2[0].RouteCode);

        // Service should only be invoked once due to HybridCache!
        mockRouteService.Verify(s => s.GetActiveRoutesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ConcurrentRequests_ForSameKey_CoalesceUnderlyingServiceCalls()
    {
        // Arrange
        var mockRouteService = new Mock<IRouteService>();
        var routeId = Guid.NewGuid();
        var expectedDto = new RouteDetailDto(routeId, "Mexico - Saris", "MEX-SAR", [], true, 3, DateTime.UtcNow);

        mockRouteService
            .Setup(s => s.GetRouteByIdAsync(routeId, It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await Task.Delay(100); // Simulate database latency
                return expectedDto;
            });

        var handler = new GetRouteByIdQueryHandler(mockRouteService.Object, _hybridCache, _cacheSettings);

        // Act: Fire 10 concurrent requests for the exact same uncached key
        var tasks = Enumerable.Range(0, 10)
            .Select(_ => handler.Handle(new GetRouteByIdQuery(routeId), CancellationToken.None))
            .ToList();

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.All(results, r => Assert.NotNull(r));
        Assert.All(results, r => Assert.Equal("MEX-SAR", r!.RouteCode));

        // HybridCache stampede protection coalesces concurrent requests into 1 underlying invocation
        mockRouteService.Verify(s => s.GetRouteByIdAsync(routeId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateRouteCommand_InvalidatesRouteCache()
    {
        // Arrange
        var mockRouteService = new Mock<IRouteService>();
        var mockAdminService = new Mock<IAdminService>();

        var sampleRoutes = new List<RouteDetailDto>
        {
            new(Guid.NewGuid(), "Old Route", "OLD-01", [], true, 0, DateTime.UtcNow)
        };

        mockRouteService
            .Setup(s => s.GetActiveRoutesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(sampleRoutes);

        var queryHandler = new GetActiveRoutesQueryHandler(mockRouteService.Object, _hybridCache, _cacheSettings);
        var createCommandHandler = new CreateRouteCommandHandler(mockAdminService.Object, _hybridCache);

        // 1. Prime cache
        await queryHandler.Handle(new GetActiveRoutesQuery(), CancellationToken.None);
        mockRouteService.Verify(s => s.GetActiveRoutesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // 2. Admin creates a new route
        var createRequest = new CreateRouteRequest("New Route", "NEW-01", [], true);
        var createdDto = new RouteDetailDto(Guid.NewGuid(), "New Route", "NEW-01", [], true, 0, DateTime.UtcNow);
        mockAdminService
            .Setup(s => s.CreateRouteAsync(createRequest, It.IsAny<CancellationToken>()))
            .ReturnsAsync(createdDto);

        await createCommandHandler.Handle(new CreateRouteCommand(createRequest), CancellationToken.None);

        // 3. Query again - should be a cache miss and call service again!
        await queryHandler.Handle(new GetActiveRoutesQuery(), CancellationToken.None);
        mockRouteService.Verify(s => s.GetActiveRoutesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task ApproveDriverCommand_InvalidatesDriverCache()
    {
        // Arrange
        var mockPassengerService = new Mock<IPassengerService>();
        var mockAdminService = new Mock<IAdminService>();
        var driverId = Guid.NewGuid();
        var driverCode = "DRV-101";

        var driverResponse = Result<DriverByCodeResponse, PassengerError>.Success(new DriverByCodeResponse
        {
            DriverId = driverCode,
            FullName = "Abebe Bikila",
            Phone = "+251911223344",
            IsActive = true
        });

        mockPassengerService
            .Setup(s => s.GetDriverByCodeAsync(driverCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(driverResponse);

        var queryHandler = new TaxiPayApi.Application.Features.Passengers.Queries.GetDriverByCode.GetDriverByCodeQueryHandler(
            mockPassengerService.Object, _hybridCache, _cacheSettings);

        var approveCommandHandler = new TaxiPayApi.Application.Features.Admins.Commands.ApproveDriver.ApproveDriverCommandHandler(
            mockAdminService.Object, _hybridCache);

        // 1. Prime cache
        var queryResult = await queryHandler.Handle(
            new TaxiPayApi.Application.Features.Passengers.Queries.GetDriverByCode.GetDriverByCodeQuery(driverCode),
            CancellationToken.None);
        Assert.True(queryResult.IsSuccess);
        mockPassengerService.Verify(s => s.GetDriverByCodeAsync(driverCode, It.IsAny<CancellationToken>()), Times.Once);

        // 2. Admin approves driver with route assignment
        mockAdminService
            .Setup(s => s.ApproveDriverAsync(driverId, It.IsAny<ApproveDriverRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        await approveCommandHandler.Handle(
            new TaxiPayApi.Application.Features.Admins.Commands.ApproveDriver.ApproveDriverCommand(driverId, new ApproveDriverRequest(null)),
            CancellationToken.None);

        // 3. Query again - should be a cache miss and call service again!
        await queryHandler.Handle(
            new TaxiPayApi.Application.Features.Passengers.Queries.GetDriverByCode.GetDriverByCodeQuery(driverCode),
            CancellationToken.None);
        mockPassengerService.Verify(s => s.GetDriverByCodeAsync(driverCode, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }

    [Fact]
    public async Task StartNewTripCommand_InvalidatesDriverCache()
    {
        // Arrange
        var mockPassengerService = new Mock<IPassengerService>();
        var mockDriverService = new Mock<IDriverService>();
        var shiftId = Guid.NewGuid();
        var driverCode = "DRV-102";

        var driverResponse = Result<DriverByCodeResponse, PassengerError>.Success(new DriverByCodeResponse
        {
            DriverId = driverCode,
            FullName = "Tadesse Tola",
            Phone = "+251911998877",
            IsActive = true
        });

        mockPassengerService
            .Setup(s => s.GetDriverByCodeAsync(driverCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(driverResponse);

        var queryHandler = new TaxiPayApi.Application.Features.Passengers.Queries.GetDriverByCode.GetDriverByCodeQueryHandler(
            mockPassengerService.Object, _hybridCache, _cacheSettings);

        var startTripHandler = new TaxiPayApi.Application.Features.Drivers.Commands.StartNewTrip.StartNewTripCommandHandler(
            mockDriverService.Object, _hybridCache);

        // 1. Prime cache
        await queryHandler.Handle(
            new TaxiPayApi.Application.Features.Passengers.Queries.GetDriverByCode.GetDriverByCodeQuery(driverCode),
            CancellationToken.None);
        mockPassengerService.Verify(s => s.GetDriverByCodeAsync(driverCode, It.IsAny<CancellationToken>()), Times.Once);

        // 2. Driver starts new trip
        mockDriverService
            .Setup(s => s.StartNewTripAsync(shiftId, "FORWARD", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<TaxiPayApi.Application.DTOs.Drivers.TripResponse, TaxiPayApi.Application.Common.DriverError>.Success(new TaxiPayApi.Application.DTOs.Drivers.TripResponse
            {
                TripId = Guid.NewGuid(),
                ShiftId = shiftId,
                Status = "Active"
            }));

        await startTripHandler.Handle(
            new TaxiPayApi.Application.Features.Drivers.Commands.StartNewTrip.StartNewTripCommand(shiftId, "FORWARD"),
            CancellationToken.None);

        // 3. Query again - should be a cache miss and hit database/service again
        await queryHandler.Handle(
            new TaxiPayApi.Application.Features.Passengers.Queries.GetDriverByCode.GetDriverByCodeQuery(driverCode),
            CancellationToken.None);
        mockPassengerService.Verify(s => s.GetDriverByCodeAsync(driverCode, It.IsAny<CancellationToken>()), Times.Exactly(2));
    }
}
