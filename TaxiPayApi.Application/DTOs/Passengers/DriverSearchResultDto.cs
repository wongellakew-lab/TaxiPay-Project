namespace TaxiPayApi.Application.DTOs.Passengers;

public class DriverSearchResultDto
{
    public string DriverId { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string RouteName { get; set; } = string.Empty;
    public string RouteCode { get; set; } = string.Empty;
    public double Rating { get; set; }
    public string? PhotoUrl { get; set; }
    public bool IsActive { get; set; }
    public bool HasActiveShift { get; set; }
}
