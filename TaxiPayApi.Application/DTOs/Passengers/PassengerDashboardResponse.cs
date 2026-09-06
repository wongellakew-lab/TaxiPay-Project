namespace TaxiPayApi.Application.DTOs.Passengers;

public record PassengerDashboardResponse
{
    public PassengerSummaryDto Summary { get; init; } = null!;
    public CurrentJourneyDto? CurrentJourney { get; init; }
    public List<RecentTripDto> RecentTrips { get; init; } = new();
    public List<FavoriteDto> Favorites { get; init; } = new();
    public PassengerStatsDto Stats { get; init; } = null!;
}

public record CurrentJourneyDto
{
    public Guid JourneyId { get; init; }
    public Guid TripId { get; init; }
    public Guid DriverId { get; init; }
    public string DriverCode { get; init; } = string.Empty;
    public string DriverName { get; init; } = string.Empty;
    public string DriverPhone { get; init; } = string.Empty;
    public string RouteName { get; init; } = string.Empty;
    public string BoardingPoint { get; init; } = string.Empty;
    public string AlightingPoint { get; init; } = string.Empty;
    public decimal Fare { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public string PaymentStatus { get; init; } = string.Empty;
    public string AlightingStatus { get; init; } = string.Empty;
    public string TripStatus { get; init; } = string.Empty;
    public DateTime BoardedAt { get; init; }
}

public record PassengerSummaryDto
{
    public string FullName { get; init; } = string.Empty;
    public string Phone { get; init; } = string.Empty;
    public decimal WalletBalance { get; init; }
    public decimal Rating { get; init; }
}

public record RecentTripDto
{
    public Guid TripId { get; init; }
    public string Direction { get; init; } = string.Empty;
    public string RouteName { get; init; } = string.Empty;
    public string DriverName { get; init; } = string.Empty;
    public string DriverId { get; init; } = string.Empty;
    public decimal Fare { get; init; }
    public DateTime TripStart { get; init; }
    public string Status { get; init; } = string.Empty;
}

public record PassengerStatsDto
{
    public int TotalTrips { get; init; }
    public decimal TotalSpent { get; init; }
    public decimal AverageFare { get; init; }
    public int TripsThisMonth { get; init; }
    public decimal SpentThisMonth { get; init; }
}