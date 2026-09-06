using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using TaxiPayApi.Application.DTOs.Drivers;
using TaxiPayApi.Application.DTOs.Passengers;
using TaxiPayApi.Domain.Entities;
using TaxiPayApi.Tests.Fixtures;

namespace TaxiPayApi.Tests.Integration.Payments;

public class PaymentAndWalletIntegrationTests : IClassFixture<TaxiPayWebApplicationFactory>
{
    private readonly TaxiPayWebApplicationFactory _factory;

    public PaymentAndWalletIntegrationTests(TaxiPayWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task PayForTrip_WhenValidWalletPayment_DeductsWalletAndCreatesReceiptInDatabase()
    {
        // Arrange
        var passengerId = Guid.NewGuid();
        var route = TestBuilders.CreateRoute();
        var fare = TestBuilders.CreateFare(route.Id, "A", "C", 25.00m);
        var driver = TestBuilders.CreateDriver(code: "ET1001", routeId: route.Id);
        var shift = TestBuilders.CreateShift(driver.Id, route.Id);
        var trip = TestBuilders.CreateTrip(shift.Id, driver.Id, route.Id);
        shift.CurrentTripId = trip.Id;
        var passenger = TestBuilders.CreatePassenger(passengerId, walletBalance: 100.00m);

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
        var request = new PayTripRequest
        {
            DriverId = driver.DriverId!,
            BoardingNode = "A",
            DestinationNode = "C",
            PaymentMethod = PaymentMethod.Wallet
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/passengers/{passengerId}/pay", request);

        // Assert - HTTP status 200 OK
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payResult = await response.Content.ReadFromJsonAsync<PayTripResponse>();
        Assert.NotNull(payResult);
        Assert.Equal(75.00m, payResult.NewBalance);
        Assert.Equal(25.00m, payResult.Fare);

        // Assert - Real database persistence verification
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var updatedPassenger = await db.Passengers.FindAsync(passengerId);
            Assert.NotNull(updatedPassenger);
            Assert.Equal(75.00m, updatedPassenger.WalletBalance);

            var journey = await db.PassengerJourneyss.FirstOrDefaultAsync(j => j.PassengerId == passengerId && j.TripId == trip.Id);
            Assert.NotNull(journey);
            Assert.Equal(25.00m, journey.Fare);
            Assert.Equal(AlightingStatus.OnBoard, journey.AlightingStatus);

            var receipt = await db.Receipts.FirstOrDefaultAsync(r => r.PassengerId == passengerId && r.TripId == trip.Id);
            Assert.NotNull(receipt);
            Assert.Equal(25.00m, receipt.Fare);
        });
    }

    [Fact]
    public async Task PayForTrip_WhenInsufficientBalance_ReturnsBadRequestAndLeavesDatabaseUnmodified()
    {
        // Arrange
        var passengerId = Guid.NewGuid();
        var route = TestBuilders.CreateRoute();
        var fare = TestBuilders.CreateFare(route.Id, "A", "C", 50.00m);
        var driver = TestBuilders.CreateDriver(code: "ET1002", routeId: route.Id);
        var shift = TestBuilders.CreateShift(driver.Id, route.Id);
        var trip = TestBuilders.CreateTrip(shift.Id, driver.Id, route.Id);
        shift.CurrentTripId = trip.Id;
        var passenger = TestBuilders.CreatePassenger(passengerId, walletBalance: 15.00m);

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
        var request = new PayTripRequest
        {
            DriverId = driver.DriverId!,
            BoardingNode = "A",
            DestinationNode = "C",
            PaymentMethod = PaymentMethod.Wallet
        };

        // Act
        var response = await client.PostAsJsonAsync($"/api/passengers/{passengerId}/pay", request);

        // Assert - HTTP status 400 Bad Request with machine-readable error code
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errorContent = await response.Content.ReadAsStringAsync();
        Assert.Contains("insufficient_balance", errorContent);

        // Assert - Real database verification: balance remains unmodified
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var unchangedPassenger = await db.Passengers.FindAsync(passengerId);
            Assert.NotNull(unchangedPassenger);
            Assert.Equal(15.00m, unchangedPassenger.WalletBalance);

            var journeyCount = await db.PassengerJourneyss.CountAsync(j => j.PassengerId == passengerId && j.TripId == trip.Id);
            Assert.Equal(0, journeyCount);
        });
    }

    [Fact]
    public async Task TopUpWallet_WhenValidAmount_IncrementsWalletBalanceInDatabase()
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
        var request = new TopUpWalletRequest { Amount = 150.00m, PaymentMethod = "Telebirr" };

        // Act
        var response = await client.PostAsJsonAsync($"/api/passengers/{passengerId}/topup", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var topUpResult = await response.Content.ReadFromJsonAsync<TopUpWalletResponse>();
        Assert.NotNull(topUpResult);
        Assert.Equal(200.00m, topUpResult.NewBalance);

        // Assert - DB verification
        await _factory.ExecuteDbContextAsync(async db =>
        {
            var updatedPassenger = await db.Passengers.FindAsync(passengerId);
            Assert.NotNull(updatedPassenger);
            Assert.Equal(200.00m, updatedPassenger.WalletBalance);
        });
    }
}
