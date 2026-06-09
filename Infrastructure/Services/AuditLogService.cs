using Application.Common;
using Application.Features.AuditLogs;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Services;

public class AuditLogService : IAuditLogService
{
    private readonly AppDbContext _context;

    public AuditLogService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<AuditLogResponseDto>> GetPagedLogsAsync(int pageNumber, int pageSize, string? term = null, string searchBy = "all")
    {
        var query = _context.AuditLogs.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(term))
        {
            term = term.Trim().ToLower();
            query = searchBy.ToLower() switch
            {
                "table" => query.Where(l => l.TableName.ToLower().Contains(term)),
                "user" => query.Where(l => l.UserId.ToLower().Contains(term)),
                "action" => query.Where(l => l.Type.ToLower().Contains(term)),
                "id" => query.Where(l => l.PrimaryKey.ToLower().Contains(term)),
                _ => query.Where(l => 
                    l.TableName.ToLower().Contains(term) || 
                    l.UserId.ToLower().Contains(term) || 
                    l.Type.ToLower().Contains(term) ||
                    l.PrimaryKey.ToLower().Contains(term))
            };
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(l => l.DateTime)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new AuditLogResponseDto
            {
                Id = l.Id,
                UserId = l.UserId,
                Type = l.Type,
                TableName = l.TableName,
                DateTime = l.DateTime,
                OldValues = l.OldValues,
                NewValues = l.NewValues,
                AffectedColumns = l.AffectedColumns,
                PrimaryKey = l.PrimaryKey
            })
            .ToListAsync();

        return new PagedResponse<AuditLogResponseDto>(items, totalCount, pageNumber, pageSize);
    }
}
