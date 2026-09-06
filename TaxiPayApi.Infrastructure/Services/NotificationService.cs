using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaxiPayApi.Application.DTOs.Notifications;
using TaxiPayApi.Application.Interfaces;
using TaxiPayApi.Domain.Entities;
using TaxiPayApi.Infrastructure.Persistence.Context;

namespace TaxiPayApi.Infrastructure.Services;

public class NotificationService : INotificationService
{
    private readonly TaxiPayDbContext _dbContext;
    private readonly ITaxiPayRealtimeNotifier _realtimeNotifier;
    private readonly ILogger<NotificationService> _logger;

    public NotificationService(
        TaxiPayDbContext dbContext,
        ITaxiPayRealtimeNotifier realtimeNotifier,
        ILogger<NotificationService> logger)
    {
        _dbContext = dbContext;
        _realtimeNotifier = realtimeNotifier;
        _logger = logger;
    }

    public async Task<List<AppNotificationDto>> GetNotificationsAsync(Guid? userId, string? role, CancellationToken ct = default)
    {
        var query = _dbContext.AppNotifications
            .AsNoTracking()
            .Where(n => !n.IsDeleted);

        if (userId.HasValue && userId.Value != Guid.Empty)
        {
            var uid = userId.Value;
            var r = (role ?? string.Empty).Trim().ToLowerInvariant();

            query = query.Where(n =>
                n.UserId == uid ||
                (n.UserId == null && (n.Role == null || n.Role.ToLower() == r || (r == "superadmin" && n.Role.ToLower() == "admin"))));
        }
        else if (!string.IsNullOrWhiteSpace(role))
        {
            var r = role.Trim().ToLowerInvariant();
            query = query.Where(n => n.Role == null || n.Role.ToLower() == r || (r == "superadmin" && n.Role.ToLower() == "admin"));
        }

        var list = await query
            .OrderByDescending(n => n.CreatedAt)
            .Take(50)
            .ToListAsync(ct);

        return list.Select(n => new AppNotificationDto(
            n.Id,
            n.UserId,
            n.Role,
            n.Title,
            n.Message,
            n.Type,
            n.TargetUrl,
            n.IsRead,
            n.CreatedAt
        )).ToList();
    }

    public async Task<AppNotificationDto> CreateNotificationAsync(
        Guid? userId,
        string? role,
        string title,
        string message,
        string type = "info",
        string? targetUrl = null,
        CancellationToken ct = default)
    {
        var notification = new AppNotification
        {
            UserId = userId,
            Role = role,
            Title = title,
            Message = message,
            Type = type,
            TargetUrl = targetUrl,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _dbContext.AppNotifications.Add(notification);
        await _dbContext.SaveChangesAsync(ct);

        var dto = new AppNotificationDto(
            notification.Id,
            notification.UserId,
            notification.Role,
            notification.Title,
            notification.Message,
            notification.Type,
            notification.TargetUrl,
            notification.IsRead,
            notification.CreatedAt
        );

        await _realtimeNotifier.NotifyNotificationCreatedAsync(dto, role, userId, ct);

        return dto;
    }

    public async Task<bool> MarkAsReadAsync(Guid notificationId, Guid? userId = null, string? role = null, CancellationToken ct = default)
    {
        var item = await _dbContext.AppNotifications.FirstOrDefaultAsync(n => n.Id == notificationId, ct);
        if (item == null) return false;

        if (userId.HasValue && item.UserId.HasValue && item.UserId.Value != userId.Value)
        {
            var isUserAdmin = string.Equals(role, "Admin", StringComparison.OrdinalIgnoreCase) ||
                              string.Equals(role, "SuperAdmin", StringComparison.OrdinalIgnoreCase);
            if (!isUserAdmin) return false;
        }

        item.IsRead = true;
        item.ReadAt = DateTime.UtcNow;
        await _dbContext.SaveChangesAsync(ct);

        return true;
    }

    public async Task<int> MarkAllAsReadAsync(Guid? userId, string? role, CancellationToken ct = default)
    {
        var query = _dbContext.AppNotifications.Where(n => !n.IsRead && !n.IsDeleted);

        if (userId.HasValue && userId.Value != Guid.Empty)
        {
            var uid = userId.Value;
            var r = (role ?? string.Empty).Trim().ToLowerInvariant();
            query = query.Where(n => n.UserId == uid || (n.UserId == null && (n.Role == null || n.Role.ToLower() == r || (r == "superadmin" && n.Role.ToLower() == "admin"))));
        }
        else if (!string.IsNullOrWhiteSpace(role))
        {
            var r = role.Trim().ToLowerInvariant();
            query = query.Where(n => n.Role == null || n.Role.ToLower() == r || (r == "superadmin" && n.Role.ToLower() == "admin"));
        }

        var unreadItems = await query.ToListAsync(ct);
        var now = DateTime.UtcNow;
        foreach (var item in unreadItems)
        {
            item.IsRead = true;
            item.ReadAt = now;
        }

        if (unreadItems.Count > 0)
        {
            await _dbContext.SaveChangesAsync(ct);
        }

        return unreadItems.Count;
    }

    public async Task<int> GetUnreadCountAsync(Guid? userId, string? role, CancellationToken ct = default)
    {
        var query = _dbContext.AppNotifications.AsNoTracking().Where(n => !n.IsRead && !n.IsDeleted);

        if (userId.HasValue && userId.Value != Guid.Empty)
        {
            var uid = userId.Value;
            var r = (role ?? string.Empty).Trim().ToLowerInvariant();
            query = query.Where(n => n.UserId == uid || (n.UserId == null && (n.Role == null || n.Role.ToLower() == r || (r == "superadmin" && n.Role.ToLower() == "admin"))));
        }
        else if (!string.IsNullOrWhiteSpace(role))
        {
            var r = role.Trim().ToLowerInvariant();
            query = query.Where(n => n.Role == null || n.Role.ToLower() == r || (r == "superadmin" && n.Role.ToLower() == "admin"));
        }

        return await query.CountAsync(ct);
    }
}
