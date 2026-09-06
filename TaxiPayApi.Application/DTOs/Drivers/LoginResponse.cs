namespace TaxiPayApi.Application.DTOs.Drivers;

public record LoginResponse
{
    public string AccessToken { get; init; } = string.Empty;
    public string RefreshToken { get; init; } = string.Empty;
    public DateTime TokenExpiresAt { get; init; }
    public DateTime LoginTime { get; init; }
    public string SessionId { get; init; } = string.Empty;
    public bool IsFirstLogin { get; init; }
    public bool RequiresPasswordChange { get; init; }
    public bool HasPendingRouteChange { get; init; }

    public DriverSummaryDto Driver { get; init; } = null!;
    public ShiftStatusDto? ActiveShift { get; init; }
}