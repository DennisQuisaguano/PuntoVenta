using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface ISaleRepository
{
    Task<string> GenerateInvoiceNumberAsync();
    Task AddAsync(Sale sale);
    Task<IEnumerable<Sale>> SearchAsync(string term, string searchBy, string? sellerName = null, DateTime? startDate = null, DateTime? endDate = null, string? status = null);
    Task<(IEnumerable<Sale> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, string? term = null, string? searchBy = null, string? sellerName = null, DateTime? startDate = null, DateTime? endDate = null, string? status = null);
    Task<Sale?> GetByIdWithDetailsAsync(int id);
    Task<IEnumerable<Sale>> GetMonthlySalesAsync(DateTime firstDayOfMonth);
    Task<int> SaveSaleAsync(Sale sale);
}
