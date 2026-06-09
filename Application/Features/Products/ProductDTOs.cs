namespace Application.Features.Products;

public class CreateProductRequest
{
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Stock { get; set; }
}

public class UpdateProductRequest
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; }
    public int Stock { get; set; }
}

public class ProductResponseDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal UnitPrice { get; set; } 
    public int Stock { get; set; }
    public bool IsActive { get; set; }
}
