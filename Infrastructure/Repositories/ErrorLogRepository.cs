using Application.Common;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class ErrorLogRepository : IErrorLogRepository
{
    private readonly AppDbContext _context;

    public ErrorLogRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResponse<ErrorLog>> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null)
    {
        IQueryable<ErrorLog> query = _context.ErrorLogs.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            searchTerm = searchTerm.ToLower();
            query = query.Where(e => 
                e.Message.ToLower().Contains(searchTerm) || 
                e.UserName!.ToLower().Contains(searchTerm) ||
                e.ExceptionType!.ToLower().Contains(searchTerm));
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(e => e.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<ErrorLog>(items, totalCount, pageNumber, pageSize);
    }

    public async Task<ErrorLog?> GetByIdAsync(int id)
    {
        return await _context.ErrorLogs.FindAsync(id);
    }

    public async Task DeleteOldLogsAsync(int daysRetained)
    {
        var cutOffDate = DateTime.UtcNow.AddDays(-daysRetained);
        var logsToDelete = await _context.ErrorLogs
            .Where(e => e.CreatedAt < cutOffDate)
            .ToListAsync();

        if (logsToDelete.Any())
        {
            _context.ErrorLogs.RemoveRange(logsToDelete);
            await _context.SaveChangesAsync();
        }
    }
}
