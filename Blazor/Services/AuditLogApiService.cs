using Application.Common;
using Application.Features.AuditLogs;
using System.Net.Http.Json;

namespace Blazor.Services;

public class AuditLogApiService
{
    private readonly HttpClient _http;

    public AuditLogApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<PagedResponse<AuditLogResponseDto>?> GetPagedLogsAsync(int pageNumber, int pageSize, string? term = null, string searchBy = "all")
    {
        try
        {
            var url = $"api/AuditLogs/paged?pageNumber={pageNumber}&pageSize={pageSize}&searchBy={Uri.EscapeDataString(searchBy)}";
            if (!string.IsNullOrWhiteSpace(term))
            {
                url += $"&term={Uri.EscapeDataString(term)}";
            }

            return await _http.GetFromJsonAsync<PagedResponse<AuditLogResponseDto>>(url);
        }
        catch { return null; }
    }
}
