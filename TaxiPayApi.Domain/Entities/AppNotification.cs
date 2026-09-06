using System.Text.Json.Serialization;

namespace TaxiPayApi.Domain.Entities;

public class AppNotification : BaseEntity
{
    public Guid? UserId { get; set; }
    public string? Role { get; set; } // "Admin", "Driver", "Passenger", or null for all
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "info"; // "info", "success", "warning", "error"
    public string? TargetUrl { get; set; }
    public bool IsRead { get; set; } = false;
    public DateTime? ReadAt { get; set; }
}
