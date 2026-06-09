using Domain.Enums;

namespace Application.Features.Sales;

public record CreateSaleDetailDto (int ProductId, int Amount);
public record CreateSaleDto(int CustomerId, List<CreateSaleDetailDto> Details);
public record SaleHeaderResponseDto (string InvoiceNumber, decimal SubTotal, decimal VatAmount, decimal Total);

public record CreateSaleRequest
{
    public int? DraftId { get; set; }
    public int CustomerId { get; set; }
    public List<CreateRequestDetail> Details { get; set; } = new();
}

public record CreateRequestDetail
{ 
    public int ProductId { get; set; }
    public int Amount { get; set; } 
}

public class SaleDetailResponseDto
{
    public int ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public int Amount { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal SubTotal { get; set; }
}

public class SaleResponseDto
{
    public int Id { get; set; }
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
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

public class SaleDetailDto
{
    public int ProductId { get; set; }
    public int Amount { get; set; }
    public decimal UnitPrice { get; set; }
}
