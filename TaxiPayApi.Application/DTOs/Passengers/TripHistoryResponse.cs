namespace TaxiPayApi.Application.DTOs.Passengers;

public record TripHistoryResponse
{
    public List<TripHistoryItemDto> Trips { get; init; } = new();
    public int TotalCount { get; init; }
    public int PageNumber { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
    public bool HasNextPage { get; init; }
    public bool HasPreviousPage { get; init; }
}

public record TripHistoryItemDto
{
    public Guid TripId { get; init; }
    public string Direction { get; init; } = string.Empty;
    public string RouteName { get; init; } = string.Empty;
    public string DriverName { get; init; } = string.Empty;
    public string DriverId { get; init; } = string.Empty;
    public string BoardingNode { get; init; } = string.Empty;
    public string DestinationNode { get; init; } = string.Empty;
    public string BoardingPointName { get; init; } = string.Empty;
    public string DestinationPointName { get; init; } = string.Empty;
    public decimal Fare { get; init; }
    public string PaymentMethod { get; init; } = string.Empty;
    public string PaymentStatus { get; init; } = string.Empty;
    public string? TransactionRef { get; init; }
    public DateTime TripStart { get; init; }
    public DateTime? TripEnd { get; init; }
    public string Status { get; init; } = string.Empty;
}