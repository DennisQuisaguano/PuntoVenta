using Domain.Enums;

namespace Blazor.Models;

public class PosModels
{
}

public class ProductModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Stock { get; set; }
}

public class CreateSaleRequest
{
    public int CustomerId { get; set; }
    public List<CreateSaleDetailRequest> Details { get; set; } = new();
}

public class CreateSaleDetailRequest
{
    public int ProductId { get; set; }
    public int Amount { get; set; }
}

public class SaleResponseDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public string IssueDate { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string CustomerIDCard { get; set; } = string.Empty;
    public string CustomerPhone { get; set; } = string.Empty;
    public string CustomerAddress { get; set; } = string.Empty;
    public string CustomerEmail { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public string SellerName { get; set; } = string.Empty;
    public string SellerLastName { get; set; } = string.Empty;
    public decimal SubTotal { get; set; }
    public decimal VatPercentage { get; set; }
    public decimal VatAmount { get; set; }
    public decimal Total { get; set; }
    public SaleStatus Status { get; set; }
    public string StatusName => Status switch
    {
        SaleStatus.Draft => "Borrador",
        SaleStatus.Confirmed => "Confirmada",
        SaleStatus.Cancelled => "Anulada",
        _ => "Desconocido"
    };

    public List<SaleDetailResponseDto> Details { get; set; } = new();
}

public class SaleDetailResponseDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Amount { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
}

public class BulkStockErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public List<StockValidationErrorModel> Errors { get; set; } = new();
}

public class ProductDeletedErrorResponse
{
    public string Message { get; set; } = string.Empty;
    public List<DeletedProductInfoModel> DeletedProducts { get; set; } = new();
}

public class StockValidationErrorModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int RequestedQuantity { get; set; }
    public int AvailableStock { get; set; }
}

public class DeletedProductInfoModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
}

public class CartItemModel
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Amount { get; set; }
    public decimal UnitPrice { get; set; }
    public int AvailableStock { get; set; }
    public decimal SubTotal => Amount * UnitPrice;

    // Objeto anidado opcional para compatibilidad con lógica de persistencia si es necesario
    public ProductModel Product { get; set; } = new();
}

public class SalesRegistryModel 
{
    public int? CustomerID { get; set; }
    public string CustomerName { get; set; } = "Final Consumer";
    public List<CartItemModel> Items { get; set; } = new();
    public decimal Subtotal { get; set; }
    public decimal Vat { get; set; }
    public decimal Total { get; set; }
    public bool HasData => Items.Any();
}
