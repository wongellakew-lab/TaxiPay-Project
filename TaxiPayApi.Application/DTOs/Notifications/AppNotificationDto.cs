namespace TaxiPayApi.Application.DTOs.Notifications;

public record AppNotificationDto(
    Guid Id,
    Guid? UserId,
    string? Role,
    string Title,
    string Message,
    string Type,
    string? TargetUrl,
    bool IsRead,
    DateTime CreatedAt
);

public record CreateNotificationRequest(
    Guid? UserId,
    string? Role,
    string Title,
    string Message,
    string Type = "info",
    string? TargetUrl = null
);
