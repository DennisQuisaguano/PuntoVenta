namespace Domain.Enums;

public enum MovementType
{
    InitialStock = 1,      // Stock inicial al crear el producto
    Replenishment = 2,     // Reabastecimiento / Compra
    Sale = 3,              // Salida por Venta
    AdjustmentAdd = 4,     // Ajuste positivo (sobrante)
    AdjustmentRemove = 5,  // Ajuste negativo (pérdida/daño)
    Return = 6             // Devolución de cliente
}
