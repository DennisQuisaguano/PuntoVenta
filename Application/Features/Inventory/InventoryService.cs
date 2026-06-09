using Application.Interfaces;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Domain.Enums;

namespace Application.Features.Inventory;

public class InventoryService : IInventoryService
{
    private readonly IProductRepository _productRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUnitOfWork _unitOfWork;

    public InventoryService(
        IProductRepository productRepository, 
        IInventoryRepository inventoryRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _inventoryRepository = inventoryRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task RestockAsync(RestockRequest request)
    {
        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var product = await _productRepository.GetByIdAsync(request.ProductId)
                ?? throw new KeyNotFoundException("Producto no encontrado.");

            int previousStock = product.Stock;
            product.IncreaseStock(request.Quantity);

            var movement = new InventoryMovement(
                product.Id,
                MovementType.Replenishment,
                request.Quantity,
                previousStock,
                product.Stock,
                request.Reference,
                request.UnitCost
            );

            await _inventoryRepository.AddMovementAsync(movement);
            
            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
    
    public async Task RegisterSaleMovementAsync(int productId, int quantity, string invoiceNumber)
    {
        var product = await _productRepository.GetByIdAsync(productId)
             ?? throw new KeyNotFoundException("Producto no encontrado.");

        int previousStock = product.Stock;

        var movement = new InventoryMovement(
            product.Id,
            MovementType.Sale,
            quantity,
            previousStock,
            product.Stock,
            $"Factura: {invoiceNumber}"
        );

        await _inventoryRepository.AddMovementAsync(movement);
    }
}
