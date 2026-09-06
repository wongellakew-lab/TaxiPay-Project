using TaxiPayApi.Domain.Entities;

namespace TaxiPayApi.Tests.Fixtures;

public static class TestBuilders
{
    public static Route CreateRoute(
        Guid? routeId = null,
        string name = "Megenagna - Tor Hailoch",
        string code = "RT-01",
        List<Node>? nodes = null)
    {
        var id = routeId ?? Guid.NewGuid();
        var routeNodes = nodes ?? new List<Node>
        {
            new() { Id = "A", Name = "Megenagna", Latitude = 9.0227, Longitude = 38.7997 },
            new() { Id = "B", Name = "Haya Hulet", Latitude = 9.0180, Longitude = 38.7850 },
            new() { Id = "C", Name = "Tor Hailoch", Latitude = 9.0100, Longitude = 38.7400 }
        };

        return new Route
        {
            Id = id,
            Name = name,
            RouteCode = code,
            Nodes = routeNodes,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static FareMatrix CreateFare(
        Guid routeId,
        string fromNode = "A",
        string toNode = "C",
        decimal fare = 25.00m)
    {
        return new FareMatrix
        {
            Id = Guid.NewGuid(),
            RouteId = routeId,
            FromNode = fromNode,
            ToNode = toNode,
            Fare = fare,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Driver CreateDriver(
        Guid? driverId = null,
        string code = "ET1001",
        string firstName = "Abebe",
        string lastName = "Bikila",
        string phone = "+251911223344",
        DriverStatus status = DriverStatus.Active,
        Guid? routeId = null)
    {
        var id = driverId ?? Guid.NewGuid();
        return new Driver
        {
            Id = id,
            UserId = Guid.NewGuid().ToString(),
            DriverId = code,
            FirstName = firstName,
            LastName = lastName,
            Phone = phone,
            LicenseNumber = $"DL-{id.ToString()[..6].ToUpper()}",
            Status = status,
            CurrentRouteId = routeId,
            Rating = 4.8m,
            IsVerified = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static DriverShift CreateShift(
        Guid driverId,
        Guid routeId,
        Guid? shiftId = null,
        ShiftStatus status = ShiftStatus.Active)
    {
        return new DriverShift
        {
            Id = shiftId ?? Guid.NewGuid(),
            DriverId = driverId,
            RouteId = routeId,
            ShiftStart = DateTime.UtcNow,
            Status = status,
            CurrentTripNumber = 0,
            TotalPassengers = 0,
            TotalRevenue = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Trip CreateTrip(
        Guid shiftId,
        Guid driverId,
        Guid routeId,
        Guid? tripId = null,
        string direction = "A→C",
        TripStatus status = TripStatus.Active,
        int passengersCount = 0)
    {
        return new Trip
        {
            Id = tripId ?? Guid.NewGuid(),
            DriverShiftId = shiftId,
            DriverId = driverId,
            RouteId = routeId,
            Direction = direction,
            TripNumber = 1,
            TripStart = DateTime.UtcNow,
            Status = status,
            PassengersCount = passengersCount,
            TripRevenue = 0,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static Passenger CreatePassenger(
        Guid? passengerId = null,
        string firstName = "Almaz",
        string lastName = "Tesfaye",
        string phone = "+251911000000",
        decimal walletBalance = 500.00m)
    {
        var id = passengerId ?? Guid.NewGuid();
        return new Passenger
        {
            Id = id,
            UserId = Guid.NewGuid().ToString(),
            FirstName = firstName,
            LastName = lastName,
            Phone = phone,
            WalletBalance = walletBalance,
            Rating = 5.0m,
            IsVerified = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static PassengerJourney CreateJourney(
        Guid tripId,
        Guid passengerId,
        Guid? journeyId = null,
        string boardingNode = "A",
        string destinationNode = "C",
        decimal fare = 25.00m,
        PaymentMethod method = PaymentMethod.Telebirr,
        PaymentStatus status = PaymentStatus.Completed,
        AlightingStatus alightingStatus = AlightingStatus.OnBoard)
    {
        return new PassengerJourney
        {
            Id = journeyId ?? Guid.NewGuid(),
            TripId = tripId,
            PassengerId = passengerId,
            BoardingNode = boardingNode,
            DestinationNode = destinationNode,
            Fare = fare,
            PaymentMethod = method,
            PaymentStatus = status,
            AlightingStatus = alightingStatus,
            TransactionRef = $"TXN-{Guid.NewGuid():N}",
            CreatedAt = DateTime.UtcNow
        };
    }
}
