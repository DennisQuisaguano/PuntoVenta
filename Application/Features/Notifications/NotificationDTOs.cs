namespace Application.Features.Notifications;

public class NotificationDto
{
    public string Message { get; set; } = string.Empty;
    public string Type { get; set; } = "Info"; // Info, Warning, Danger
    public string Icon { get; set; } = "bi-info-circle";
    public DateTime CreatedAt { get; set; }
    public string? TargetUrl { get; set; }
}
