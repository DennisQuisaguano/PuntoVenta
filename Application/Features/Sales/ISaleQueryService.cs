using Domain.Entities;

namespace Application.Features.Sales;

public interface ISaleQueryService
{
    Task<IEnumerable<Sale>> ExecuteAsync(string term, string searchBy, string? sellerName = null, DateTime? startDate = null, DateTime? endDate = null, string? status = null);
    Task<(IEnumerable<Sale> Items, int TotalCount)> ExecutePagedAsync(
        int page, 
        int pageSize, 
        string? term, 
        string? searchBy, 
        string? sellerName = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? status = null);
}
