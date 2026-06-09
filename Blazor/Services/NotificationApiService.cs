using Application.Features.Notifications;
using System.Net.Http.Json;

namespace Blazor.Services;

public class NotificationApiService
{
    private readonly HttpClient _http;
    public NotificationApiService(HttpClient http) => _http = http;

    public async Task<List<NotificationDto>> GetNotificationsAsync()
    {
        try
        {
            return await _http.GetFromJsonAsync<List<NotificationDto>>("api/Notifications") ?? new();
        }
        catch
        {
            return new List<NotificationDto>();
        }
    }
}
