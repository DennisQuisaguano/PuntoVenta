using Application.Features.ErrorLogs;
using Application.Common;
using System.Net.Http.Json;

namespace Blazor.Services;

public class ErrorLogApiService
{
    private readonly HttpClient _http;

    public ErrorLogApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<PagedResponse<ErrorLogResponseDto>?> GetPagedLogsAsync(int page, int size, string? searchTerm = null)
    {
        try
        {
            var url = $"api/ErrorLogs?page={page}&size={size}";
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                url += $"&searchTerm={Uri.EscapeDataString(searchTerm)}";
            }

            return await _http.GetFromJsonAsync<PagedResponse<ErrorLogResponseDto>>(url);
        }
        catch
        {
            return null;
        }
    }

    public async Task<ErrorLogResponseDto?> GetLogByIdAsync(int id)
    {
        try
        {
            return await _http.GetFromJsonAsync<ErrorLogResponseDto>($"api/ErrorLogs/{id}");
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> ClearOldLogsAsync(int days)
    {
        try
        {
            var response = await _http.DeleteAsync($"api/ErrorLogs/clear-old/{days}");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
