using Domain.Entities;

namespace Application.Interfaces.Repositories;

public interface IInventoryRepository
{
    Task AddMovementAsync(InventoryMovement movement);
    Task<IEnumerable<InventoryMovement>> GetProductMovementsAsync(int productId);
}
