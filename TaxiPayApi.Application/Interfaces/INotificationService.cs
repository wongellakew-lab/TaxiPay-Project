using TaxiPayApi.Application.DTOs.Notifications;

namespace TaxiPayApi.Application.Interfaces;

public interface INotificationService
{
    Task<List<AppNotificationDto>> GetNotificationsAsync(Guid? userId, string? role, CancellationToken ct = default);
    Task<AppNotificationDto> CreateNotificationAsync(Guid? userId, string? role, string title, string message, string type = "info", string? targetUrl = null, CancellationToken ct = default);
    Task<bool> MarkAsReadAsync(Guid notificationId, Guid? userId = null, string? role = null, CancellationToken ct = default);
    Task<int> MarkAllAsReadAsync(Guid? userId, string? role, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(Guid? userId, string? role, CancellationToken ct = default);
}
