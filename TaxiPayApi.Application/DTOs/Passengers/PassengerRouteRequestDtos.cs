namespace TaxiPayApi.Application.DTOs.Passengers;

public class CreatePassengerRouteRequestDto
{
    public required string FromLocation { get; set; }
    public required string ToLocation { get; set; }
    public string Description { get; set; } = string.Empty;
}

public class PassengerRouteRequestDto
{
    public Guid Id { get; set; }
    public Guid PassengerId { get; set; }
    public string PassengerName { get; set; } = string.Empty;
    public string PassengerPhone { get; set; } = string.Empty;
    public string FromLocation { get; set; } = string.Empty;
    public string ToLocation { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public DateTime RequestedAt { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? AdminNotes { get; set; }
}
