using Domain.Enums;

namespace Application.Features.Inventory;

public class InventoryMovementResponseDto
{
    public int Id { get; set; }
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public MovementType Type { get; set; }
    public string TypeName => Type switch
    {
        MovementType.InitialStock => "Stock Inicial",
        MovementType.Replenishment => "Reabastecimiento",
        MovementType.Sale => "Venta",
        MovementType.AdjustmentAdd => "Ajuste (+)",
        MovementType.AdjustmentRemove => "Ajuste (-)",
        MovementType.Return => "Devolución",
        _ => "Desconocido"
    };
    public int Quantity { get; set; }
    public int PreviousStock { get; set; }
    public int NewStock { get; set; }
    public decimal? UnitCost { get; set; }
    public string Reference { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
}

public class RestockRequest
{
    public int ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public string Reference { get; set; } = string.Empty;
}
