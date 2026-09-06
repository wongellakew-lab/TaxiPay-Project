using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TaxiPayApi.Application.DTOs.Admin;
using TaxiPayApi.Domain.Entities;
using TaxiPayApi.Tests.Fixtures;

namespace TaxiPayApi.Tests.Integration.Controllers;

public class AdminsControllerIntegrationTests : IClassFixture<TaxiPayWebApplicationFactory>
{
    private readonly TaxiPayWebApplicationFactory _factory;

    public AdminsControllerIntegrationTests(TaxiPayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task ApproveDriver_WhenPending_UpdatesDriverStatusToActiveAndAssignsDriverCode()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var route = TestBuilders.CreateRoute();
        var driver = new Driver
        {
            Id = driverId,
            FirstName = "Bekele",
            LastName = "Girma",
            Phone = "+251911334455",
            LicenseNumber = "DL-9988",
            Status = DriverStatus.Pending,
            PreferredRouteId = route.Id,
            CreatedAt = DateTime.UtcNow
        };

        await _factory.ExecuteDbContextAsync(async db =>
        {
            db.Routes.Add(route);
            db.Drivers.Add(driver);
            await db.SaveChangesAsync();
        });

        var adminClient = _factory.CreateAdminClient(adminId);
        var approveDto = new ApproveDriverRequest(route.Id);

        // Act
        var response = await adminClient.PostAsJsonAsync($"/api/admins/drivers/{driverId}/approve", approveDto);

        // Assert - HTTP response
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Assert - DB verification
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var updatedDriver = await db.Drivers.FindAsync(driverId);
            Assert.NotNull(updatedDriver);
            Assert.Equal(DriverStatus.Active, updatedDriver.Status);
            Assert.True(updatedDriver.IsVerified);
            Assert.NotNull(updatedDriver.DriverId); // Driver code generated
            Assert.Equal(route.Id, updatedDriver.CurrentRouteId);
        });
    }

    [Fact]
    public async Task RejectDriver_WithReason_PersistsRejectedStatusAndReasonInDatabase()
    {
        // Arrange
        var adminId = Guid.NewGuid();
        var driverId = Guid.NewGuid();
        var driver = new Driver
        {
            Id = driverId,
            FirstName = "Kassahun",
            LastName = "Kebede",
            Phone = "+251911445566",
            LicenseNumber = "DL-7766",
            Status = DriverStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _factory.ExecuteDbContextAsync(async db =>
        {
            db.Drivers.Add(driver);
            await db.SaveChangesAsync();
        });

        var adminClient = _factory.CreateAdminClient(adminId);
        var rejectDto = new RejectDriverRequest("Expired driving license document");

        // Act
        var response = await adminClient.PostAsJsonAsync($"/api/admins/drivers/{driverId}/reject", rejectDto);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        // Assert - DB verification
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var updatedDriver = await db.Drivers.FindAsync(driverId);
            Assert.NotNull(updatedDriver);
            Assert.Equal(DriverStatus.Rejected, updatedDriver.Status);
            Assert.Equal("Expired driving license document", updatedDriver.RejectionReason);
        });
    }
}
