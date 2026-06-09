namespace Application.Features.Notifications;

public interface INotificationService
{
    Task<IEnumerable<NotificationDto>> GetNotificationsAsync(int? userId, string role, string? userName = null);
}
