using Application.Common;
using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int id);
    Task<Customer?> GetByIdCardAsync(string IDCard);
    Task<Customer?> GetByIdCardIncludingInactiveAsync(string IDCard);

    Task<IEnumerable<Customer>> SearchAsync(string term, string searchBy);


    Task<PagedResponse<Customer>> GetPagedAsync(
        int pageNumber, 
        int pageSize, 
        string? term = null,
        string searchBy = "name",
        string status = "active"); // "active", "inactive", "all"

    Task AddAsync(Customer customer);
    Task UpdateAsync(Customer customer);
    Task DeleteAsync(int id);
    Task<bool> HasRelatedRecordsAsync(int id);
}
