using Application.Features.Notifications;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class NotificationsController : ControllerBase
{
    private readonly INotificationService _notificationService;

    public NotificationsController(INotificationService notificationService)
    {
        _notificationService = notificationService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<NotificationDto>>> Get()
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        int? userId = string.IsNullOrEmpty(userIdStr) ? null : int.Parse(userIdStr);
        
        var role = User.FindFirstValue(ClaimTypes.Role) ?? "Vendedor";
        var userName = User.Identity?.Name;

        var notifications = await _notificationService.GetNotificationsAsync(userId, role, userName);
        return Ok(notifications);
    }
}
