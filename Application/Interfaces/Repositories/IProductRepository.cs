using Application.Common;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IProductRepository
{
    Task<IEnumerable<Product>> GetByIdsAsync(IEnumerable<int> ids);
    Task<Product?> GetByIdAsync(int id);
    Task<Product?> GetByNameAsync(string name);
    Task UpdateRangeAsync(IEnumerable<Product> products);
    Task<IEnumerable<Product>> SearchAsync(string term, string searchBy);
    Task<PagedResponse<Product>> GetPagedAsync(int pageNumber, int pageSize, string? term = null, string searchBy = "name", string status = "active");
    Task AddAsync(Product product);
    Task UpdateAsync(Product product);
    Task DeleteAsync(int id);
    Task<bool> HasRelatedRecordsAsync(int id);
    Task ReactivateAsync(int id);
}
