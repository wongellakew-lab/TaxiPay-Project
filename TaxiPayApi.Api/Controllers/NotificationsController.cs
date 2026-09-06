using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaxiPayApi.Application.DTOs.Notifications;
using TaxiPayApi.Application.Interfaces;

namespace TaxiPayApi.Api.Controllers;

[ApiController]
[Route("api/notifications")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;
    private readonly ILogger<NotificationsController> _logger;

    public NotificationsController(
        INotificationService notificationService,
        ILogger<NotificationsController> logger)
    {
        _notificationService = notificationService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(List<AppNotificationDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Get user notifications")]
    [EndpointDescription("Fetches persistent notifications for the authenticated user and their active role, ordered by recency.")]
    public async Task<IActionResult> GetNotifications(CancellationToken ct)
    {
        var (userId, role) = GetUserContext();
        var notifications = await _notificationService.GetNotificationsAsync(userId, role, ct);
        return Ok(notifications);
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(int), StatusCodes.Status200OK)]
    [EndpointSummary("Get unread notifications count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
    {
        var (userId, role) = GetUserContext();
        var count = await _notificationService.GetUnreadCountAsync(userId, role, ct);
        return Ok(new { unreadCount = count });
    }

    [HttpPost("{id:guid}/read")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [EndpointSummary("Mark notification as read")]
    public async Task<IActionResult> MarkAsRead([FromRoute] Guid id, CancellationToken ct)
    {
        var (userId, role) = GetUserContext();
        var success = await _notificationService.MarkAsReadAsync(id, userId, role, ct);
        if (!success) return NotFound();
        return Ok(new { message = "Notification marked as read." });
    }

    [HttpPost("read-all")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [EndpointSummary("Mark all notifications as read")]
    public async Task<IActionResult> MarkAllAsRead(CancellationToken ct)
    {
        var (userId, role) = GetUserContext();
        var count = await _notificationService.MarkAllAsReadAsync(userId, role, ct);
        return Ok(new { markedCount = count });
    }

    private (Guid? userId, string? role) GetUserContext()
    {
        var passengerIdStr = User.FindFirstValue("passengerId");
        var driverGuidStr = User.FindFirstValue("driverGuid");
        var adminGuidStr = User.FindFirstValue("adminGuid");
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var role = User.FindFirstValue(ClaimTypes.Role) ?? User.FindFirstValue("role");

        Guid? userId = null;
        if (!string.IsNullOrEmpty(passengerIdStr) && Guid.TryParse(passengerIdStr, out var pid))
        {
            userId = pid;
        }
        else if (!string.IsNullOrEmpty(driverGuidStr) && Guid.TryParse(driverGuidStr, out var dgid))
        {
            userId = dgid;
        }
        else if (!string.IsNullOrEmpty(adminGuidStr) && Guid.TryParse(adminGuidStr, out var agid))
        {
            userId = agid;
        }
        else if (!string.IsNullOrEmpty(userIdStr) && Guid.TryParse(userIdStr, out var parsedGuid))
        {
            userId = parsedGuid;
        }

        return (userId, role);
    }
}
