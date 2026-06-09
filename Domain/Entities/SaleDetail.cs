using Domain.ValueObjects;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;

public class SaleDetail
{
    public int Id { get; private set; }
    public int ProductId { get; private set; }
    public string ProductName { get; private set; } = string.Empty; // Snapshot del nombre
    public int Amount { get; private set; }
    public Price UnitPrice { get; private set; }
    public virtual Product Product { get; set; }
    [ForeignKey("ProductId")]
    public Price SubTotal => UnitPrice * Amount;

    private SaleDetail() { }

    public SaleDetail(int productId, string productName, int amount, Price unitPrice)
    {
        ProductId = productId;
        ProductName = productName;
        Amount = amount;
        UnitPrice = unitPrice;
    }
}
