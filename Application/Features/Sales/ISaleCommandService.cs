using Domain.Entities;

namespace Application.Features.Sales;

public interface ISaleCommandService
{
    Task<Sale> CreateSaleAsync(CreateSaleRequest request);
    Task ConfirmSaleAsync(int saleId);
    Task CancelSaleAsync(int saleId);
}
