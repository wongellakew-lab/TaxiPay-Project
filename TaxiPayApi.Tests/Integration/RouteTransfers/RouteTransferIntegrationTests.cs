using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Domain.Entities;
using TaxiPayApi.Tests.Fixtures;

namespace TaxiPayApi.Tests.Integration.RouteTransfers;

public class RouteTransferIntegrationTests : IClassFixture<TaxiPayWebApplicationFactory>
{
    private readonly TaxiPayWebApplicationFactory _factory;

    public RouteTransferIntegrationTests(TaxiPayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task RequestRouteChange_WhenValid_PersistsPendingRequestInDatabase()
    {
        // Arrange
        var driverId = Guid.NewGuid();
        var route1 = TestBuilders.CreateRoute();
        var route2 = TestBuilders.CreateRoute();
        var driver = TestBuilders.CreateDriver(driverId, "ET-TX-3001", routeId: route1.Id);

        await _factory.ExecuteDbContextAsync(async db =>
        {
            db.Routes.AddRange(route1, route2);
            db.Drivers.Add(driver);
            await db.SaveChangesAsync();
        });

        var client = _factory.CreateDriverClient(driverId, "ET-TX-3001");
        var request = new RequestRouteChangeRequest
        {
            NewRouteId = route2.Id,
            Reason = "Relocating to new station"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/drivers/{driverId}/route-change/request", request);

        // Assert - HTTP response
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var changeDto = await response.Content.ReadFromJsonAsync<RouteChangeResponse>();
        Assert.NotNull(changeDto);
        Assert.Equal("Pending", changeDto.Status);

        // Assert - DB verification
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var requestInDb = await db.RouteChangeRequests.FirstOrDefaultAsync(r => r.DriverId == driverId);
            Assert.NotNull(requestInDb);
            Assert.Equal(RouteChangeStatus.Pending, requestInDb.Status);
            Assert.Equal(route2.Id, requestInDb.NewRouteId);
            Assert.Equal("Relocating to new station", requestInDb.Reason);
        });
    }

    [Fact]
    public async Task AdminReviewRouteChange_WhenApproved_UpdatesDriverRouteInDatabase()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var route1 = TestBuilders.CreateRoute();
        var route2 = TestBuilders.CreateRoute();
        var driver = TestBuilders.CreateDriver(driverId, "ET-TX-3002", routeId: route1.Id);
        var changeRequest = new RouteChangeRequest
        {
            DriverId = driverId,
            CurrentRouteId = route1.Id,
            NewRouteId = route2.Id,
            Reason = "Operational shift",
            Status = RouteChangeStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _factory.ExecuteDbContextAsync(async db =>
        {
            db.Routes.AddRange(route1, route2);
            db.Drivers.Add(driver);
            db.RouteChangeRequests.Add(changeRequest);
            await db.SaveChangesAsync();
        });

        var adminClient = _factory.CreateAdminClient(adminId);
        var reviewDto = new ReviewRouteChangeRequestDto(
            Approved: true,
            Note: "Approved for next shift"
        );

        // Act
        var response = await adminClient.PostAsJsonAsync($"/api/admins/route-change-requests/{changeRequest.Id}/review", reviewDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Assert - DB verification
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var updatedRequest = await db.RouteChangeRequests.FindAsync(changeRequest.Id);
            Assert.NotNull(updatedRequest);
            Assert.Equal(RouteChangeStatus.Approved, updatedRequest.Status);

            var updatedDriver = await db.Drivers.FindAsync(driverId);
            Assert.NotNull(updatedDriver);
            Assert.Equal(route2.Id, updatedDriver.PreferredRouteId);
        });
    }
}
