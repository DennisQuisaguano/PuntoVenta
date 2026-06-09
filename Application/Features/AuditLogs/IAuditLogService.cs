using Application.Common;

namespace Application.Features.AuditLogs;

public interface IAuditLogService
{
    Task<PagedResponse<AuditLogResponseDto>> GetPagedLogsAsync(int pageNumber, int pageSize, string? term = null, string searchBy = "all");
}
