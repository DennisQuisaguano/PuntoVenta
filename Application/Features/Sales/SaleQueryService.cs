using Application.Interfaces.Repositories;
using Domain.Entities;

namespace Application.Features.Sales;

public class SaleQueryService : ISaleQueryService
{
    private readonly ISaleRepository _saleRepository;

    public SaleQueryService(ISaleRepository saleRepository)
    {
        _saleRepository = saleRepository;
    }

    public async Task<IEnumerable<Sale>> ExecuteAsync(string term, string searchBy, string? sellerName = null, DateTime? startDate = null, DateTime? endDate = null, string? status = null)
    {
        return await _saleRepository.SearchAsync(term, searchBy, sellerName, startDate, endDate, status);
    }

    public async Task<(IEnumerable<Sale> Items, int TotalCount)> ExecutePagedAsync(
        int page, 
        int pageSize, 
        string? term, 
        string? searchBy, 
        string? sellerName = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? status = null)
    {
        return await _saleRepository.GetPagedAsync(page, pageSize, term, searchBy, sellerName, startDate, endDate, status);
    }
}
