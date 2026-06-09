using Domain.Common;
using Domain.ValueObjects;
using Domain.Enums;
using System.ComponentModel.DataAnnotations.Schema;

namespace Domain.Entities;
public class Sale : AuditableEntity
{
    public int Id { get; private set; }
    public DateTime IssueDate { get; private set; }
    public string InvoiceNumber { get; private set; } = string.Empty;
    public int CustomerId { get; private set; }
    
    // Snapshots del Cliente al momento de la venta
    public string CustomerName { get; private set; } = string.Empty;
    public string CustomerLastName { get; private set; } = string.Empty;
    public string CustomerIDCard { get; private set; } = string.Empty;
    public string CustomerAddress { get; private set; } = string.Empty;
    public string CustomerPhone { get; private set; } = string.Empty;
    public string CustomerEmail { get; private set; } = string.Empty;

    // Snapshots del Vendedor (Usuario que realiza la acción)
    public string SellerName { get; private set; } = string.Empty;
    public string SellerLastName { get; private set; } = string.Empty;

    [ForeignKey("CustomerId")]
    public virtual Customer? Customer { get; private set; }

    public decimal SubTotal { get; private set; }
    public decimal VatPercentage { get; private set; } 
    public decimal VatAmount { get; private set; }
    public decimal Total { get; private set; }
    public SaleStatus Status { get; private set; }

    private readonly List<SaleDetail> _details = new();
    public IReadOnlyCollection<SaleDetail> Details => _details.AsReadOnly();

    protected Sale() { }

    public Sale(string invoiceNumber, Customer customer, ApplicationUser seller, decimal vatRate)
    {
        InvoiceNumber = invoiceNumber;
        VatPercentage = vatRate;
        Status = SaleStatus.Draft;

        var ecuadorTimeZone = TimeZoneInfo.FindSystemTimeZoneById("SA Pacific Standard Time");
        IssueDate = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ecuadorTimeZone);

        UpdateCustomerSnapshot(customer);
        UpdateSellerSnapshot(seller);
    }

    public void UpdateCustomerSnapshot(Customer customer)
    {
        if (customer == null) throw new ArgumentNullException(nameof(customer));
        
        CustomerId = customer.Id;
        CustomerName = customer.Name;
        CustomerLastName = customer.LastName;
        CustomerIDCard = customer.IDCard;
        CustomerAddress = customer.Address;
        CustomerPhone = customer.Phone;
        CustomerEmail = customer.Email;
    }

    public void UpdateSellerSnapshot(ApplicationUser seller)
    {
        if (seller == null) throw new ArgumentNullException(nameof(seller));

        SellerName = seller.FirstName;
        SellerLastName = seller.LastName;
    }

    public void AddDetail(Product product, int amount)
    {
        if (Status != SaleStatus.Draft)
            throw new InvalidOperationException("No se pueden agregar productos a una venta ya confirmada o anulada.");

        var detail = new SaleDetail(product.Id, product.Name, amount, product.UnitPrice);
        _details.Add(detail);
        UpdateTotals();
    }

    public void ClearDetails()
    {
        if (Status != SaleStatus.Draft)
            throw new InvalidOperationException("Solo se pueden modificar productos de una venta en Borrador.");
        _details.Clear();
        UpdateTotals();
    }

    public void UpdateCustomer(int customerId)
    {
        if (Status != SaleStatus.Draft)
            throw new InvalidOperationException("Solo se pueden modificar el cliente de una venta en Borrador.");
        CustomerId = customerId;
    }

    public void UpdateTotals()
    {
        SubTotal = _details.Sum(d => d.SubTotal.Worth);
        VatAmount = Math.Round(SubTotal * (VatPercentage / 100m), 2, MidpointRounding.AwayFromZero);
        Total = SubTotal + VatAmount;
    }

    public void Confirm()
    {
        if (Status != SaleStatus.Draft)
            throw new InvalidOperationException("Solo se puede confirmar una venta que esté en estado Borrador.");
        
        if (!_details.Any())
            throw new InvalidOperationException("No se puede confirmar una venta sin productos.");

        Status = SaleStatus.Confirmed;
    }

    public void Cancel()
    {
        if (Status == SaleStatus.Cancelled)
            throw new InvalidOperationException("La venta ya está anulada.");

        Status = SaleStatus.Cancelled;
    }
}
