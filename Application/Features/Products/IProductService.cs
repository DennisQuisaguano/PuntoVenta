using Application.Common;
using Domain.Entities;

namespace Application.Features.Products;

public interface IProductService
{
    Task<PagedResponse<ProductResponseDto>> GetPagedProducts(int pageNumber, int pageSize, string? term, string searchBy);
    Task<Product> CreateProductAsync(CreateProductRequest request);
    Task UpdateProductAsync(UpdateProductRequest request);
    Task DeleteProductAsync(int id);
    Task ReactivateProductAsync(int id);
}
