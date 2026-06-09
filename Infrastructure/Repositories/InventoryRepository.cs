using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class InventoryRepository : IInventoryRepository
{
    private readonly AppDbContext _context;

    public InventoryRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task AddMovementAsync(InventoryMovement movement)
    {
        await _context.InventoryMovements.AddAsync(movement);
        // El SaveChanges se delega al Handler para asegurar atomicidad si hay múltiples cambios
    }

    public async Task<IEnumerable<InventoryMovement>> GetProductMovementsAsync(int productId)
    {
        return await _context.InventoryMovements
            .AsNoTracking()
            .Where(m => m.ProductId == productId)
            .OrderByDescending(m => m.CreatedAt)
            .ToListAsync();
    }
}
