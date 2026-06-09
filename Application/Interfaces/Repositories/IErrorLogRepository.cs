using Application.Common;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IErrorLogRepository
{
    Task<PagedResponse<ErrorLog>> GetPagedAsync(int pageNumber, int pageSize, string? searchTerm = null);
    Task<ErrorLog?> GetByIdAsync(int id);
    Task DeleteOldLogsAsync(int daysRetained);
}
