using Domain.Entities;

namespace Application.Features.Customers;

public interface ICustomerService
{
    Task<Customer> CreateCustomerAsync(CreateCustomerRequest request);
    Task ReactivateCustomerAsync(int customerId, UpdateCustomerRequest request);
    Task UpdateCustomerAsync(UpdateCustomerRequest request);
    Task DeleteCustomerAsync(int id);
}
