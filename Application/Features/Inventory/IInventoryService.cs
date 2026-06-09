namespace Application.Features.Inventory;

public interface IInventoryService
{
    Task RestockAsync(RestockRequest request);
    Task RegisterSaleMovementAsync(int productId, int quantity, string invoiceNumber);
}
