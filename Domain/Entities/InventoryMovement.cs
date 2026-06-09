using Domain.Common;
using Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public class InventoryMovement : AuditableEntity
{
    public int Id { get; private set; }
    public int ProductId { get; private set; }
    [ForeignKey("ProductId")]
    public virtual Product? Product { get; private set; }

    public MovementType Type { get; private set; }
    public int Quantity { get; private set; } // Variación absoluta
    public int PreviousStock { get; private set; }
    public int NewStock { get; private set; }
    
    public decimal? UnitCost { get; private set; } 
    public string Reference { get; private set; } = string.Empty;

    protected InventoryMovement() { }

    public InventoryMovement(
        int productId, 
        MovementType type, 
        int quantity, 
        int previousStock, 
        int newStock, 
        string? reference = "", 
        decimal? unitCost = null)
    {
        ProductId = productId;
        Type = type;
        Quantity = quantity;
        PreviousStock = previousStock;
        NewStock = newStock;
        Reference = reference ?? string.Empty; // Asegurar que no sea null para la DB
        UnitCost = unitCost;
        
        // Asegurar que la entidad esté activa para el filtro global
        Activate();
    }
}
