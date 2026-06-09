using Domain.Common;
using Domain.ValueObjects;

namespace Domain.Entities;

public class Product : AuditableEntity
{
    public int Id { get; set; }
    public string Name { get; private set; } = string.Empty;
    public Price UnitPrice { get; private set; }
    public int Stock { get; private set; }

    protected Product() { }

    public Product(string name, Price unitPrice, int initialStock)
    {
        if (initialStock < 0)
            throw new ArgumentException("El stock inicial no puede ser negativo.");

        Name = name.Trim().ToUpper();
        UnitPrice = unitPrice;
        Stock = initialStock;
    }

    public void UpdateInfo(string name, Price unitPrice)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("El nombre del producto es obligatorio.");
        
        Name = name.Trim().ToUpper();
        UnitPrice = unitPrice;
    }

    public void IncreaseStock(int amount)
    {
        if (amount <= 0)
            throw new ArgumentException("La cantidad a ingresar debe ser mayor a cero.");

        Stock += amount;
    }

    public void DecreaseStock(int amount)
    {
        if (amount <= 0)
            throw new ArgumentException("La cantidad a descontar debe ser mayor a cero.");

        if (amount > Stock)
            throw new ArgumentException($"Stock insuficiente para el producto '{Name}'. Disponible '{Stock}'. Solicitado '{amount}'.");

        Stock -= amount;
    }
}
