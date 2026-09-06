using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TaxiPayApi.Application.DTOs.Passengers;
using TaxiPayApi.Domain.Entities;
using TaxiPayApi.Tests.Fixtures;

namespace TaxiPayApi.Tests.Integration;

public class PassengerApiIntegrationTests : IClassFixture<TaxiPayWebApplicationFactory>
{
    private readonly TaxiPayWebApplicationFactory _factory;

    public PassengerApiIntegrationTests(TaxiPayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PayForTrip_EndToEnd_Returns200AndPersistsChangesInDatabase()
    {
        // Arrange
        var passengerId = Guid.NewGuid();
        var route = TestBuilders.CreateRoute();
        var fare = TestBuilders.CreateFare(route.Id, "A", "C", 25.00m);
        var uniqueDriverCode = $"D{Guid.NewGuid():N}"[..6].ToUpper();
        var driver = TestBuilders.CreateDriver(code: uniqueDriverCode, routeId: route.Id);
        var shift = TestBuilders.CreateShift(driver.Id, route.Id);
        var trip = TestBuilders.CreateTrip(shift.Id, driver.Id, route.Id);
        shift.CurrentTripId = trip.Id;
        var passenger = TestBuilders.CreatePassenger(passengerId, walletBalance: 200.00m);

        await _factory.ExecuteDbContextAsync(async db =>
        {
            db.Routes.Add(route);
            db.FareMatrices.Add(fare);
            db.Drivers.Add(driver);
            db.DriverShifts.Add(shift);
            db.Trips.Add(trip);
            db.Passengers.Add(passenger);
            await db.SaveChangesAsync();
        });

        var client = _factory.CreatePassengerClient(passengerId);
        var paymentRequest = new PayTripRequest
        {
            DriverId = driver.DriverId!,
            BoardingNode = "A",
            DestinationNode = "C",
            PaymentMethod = PaymentMethod.Wallet
        };

        // Act: Call the real HTTP pipeline
        var response = await client.PostAsJsonAsync($"/api/passengers/{passengerId}/pay", paymentRequest);

        // Assert HTTP Response
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<PayTripResponse>();
        Assert.NotNull(result);
        Assert.Equal(25.00m, result.Fare);
        Assert.Equal(175.00m, result.NewBalance);
        Assert.StartsWith("RCP-", result.ReceiptNumber);

        // Assert Database State: Verify wallet deducted, revenue credited, receipt persisted
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var dbPassenger = await db.Passengers.FindAsync(passengerId);
            Assert.Equal(175.00m, dbPassenger!.WalletBalance);
            Assert.Equal(25.00m, dbPassenger.TotalSpent);

            var dbTrip = await db.Trips.FindAsync(trip.Id);
            Assert.Equal(25.00m, dbTrip!.TripRevenue);
            Assert.Equal(1, dbTrip.PassengersCount);

            var dbJourney = await db.PassengerJourneyss.FirstOrDefaultAsync(j => j.TripId == trip.Id);
            Assert.NotNull(dbJourney);
            Assert.Equal(PaymentStatus.Completed, dbJourney.PaymentStatus);
            Assert.Equal(AlightingStatus.OnBoard, dbJourney.AlightingStatus);

            var dbReceipt = await db.Receipts.FirstOrDefaultAsync(r => r.JourneyId == dbJourney.Id);
            Assert.NotNull(dbReceipt);
            Assert.Equal(result.ReceiptNumber, dbReceipt.ReceiptNumber);
        });
    }

    [Fact]
    public async Task TopUpWallet_EndToEnd_Returns200AndUpdatesDatabaseBalance()
    {
        // Arrange
        var passengerId = Guid.NewGuid();
        var passenger = TestBuilders.CreatePassenger(passengerId, walletBalance: 50.00m);

        await _factory.ExecuteDbContextAsync(async db =>
        {
            db.Passengers.Add(passenger);
            await db.SaveChangesAsync();
        });

        var client = _factory.CreatePassengerClient(passengerId);
        var topUpRequest = new TopUpWalletRequest
        {
            Amount = 150.00m,
            PaymentMethod = "Telebirr"
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/passengers/{passengerId}/topup", topUpRequest);

        // Assert HTTP Response
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = await response.Content.ReadFromJsonAsync<TopUpWalletResponse>();
        Assert.NotNull(result);
        Assert.Equal(200.00m, result.NewBalance);

        // Assert Database State
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var dbPassenger = await db.Passengers.FindAsync(passengerId);
            Assert.Equal(200.00m, dbPassenger!.WalletBalance);
        });
    }

    [Fact]
    public async Task GetDashboard_EndToEnd_ReturnsAccurateDatabaseValues()
    {
        // Arrange
        var passengerId = Guid.NewGuid();
        var passenger = TestBuilders.CreatePassenger(passengerId, walletBalance: 320.00m);

        await _factory.ExecuteDbContextAsync(async db =>
        {
            db.Passengers.Add(passenger);
            await db.SaveChangesAsync();
        });

        var client = _factory.CreatePassengerClient(passengerId);

        // Act
        var response = await client.GetAsync($"/api/passengers/{passengerId}/dashboard");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dashboard = await response.Content.ReadFromJsonAsync<PassengerDashboardResponse>();
        Assert.NotNull(dashboard);
        Assert.Equal(320.00m, dashboard.Summary.WalletBalance);
    }
}
