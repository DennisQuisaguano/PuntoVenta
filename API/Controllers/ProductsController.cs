using Application.Common;
using Application.Features.Products;
using Application.Interfaces.Repositories;
using Infrastructure.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductsController : ControllerBase
{
    private readonly IProductRepository _productsRepository;
    private readonly IProductService _productService;

    public ProductsController(IProductRepository productsRepository, IProductService productService)
    {
        _productsRepository = productsRepository;
        _productService = productService;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] string term, [FromQuery] string searchBy = "name")
    {
        if (string.IsNullOrWhiteSpace(term))
            return BadRequest(new { message = "Search term is mandatory." });

        var results = await _productsRepository.SearchAsync(term, searchBy);

        var response = results.Select(p => new ProductResponseDto
        {
            Id = p.Id,
            Name = p.Name,
            UnitPrice = p.UnitPrice.Worth,
            Stock = p.Stock,
            IsActive = p.IsActive
        });

        return Ok(response);
    }

    [HttpGet("paged")]
    public async Task<ActionResult<PagedResponse<ProductResponseDto>>> GetPaged(
    [FromQuery] int pageNumber = 1,
    [FromQuery] int pageSize = 10,
    [FromQuery] string? term = null,
    [FromQuery] string searchBy = "name",
    [FromQuery] string status = "active")
    {
        if (status != "active" && !User.IsInRole("Administrador"))
        if (status != "active" && !User.IsInRole("Administrador"))
        {
            status = "active";
        }

        var result = await _productsRepository.GetPagedAsync(pageNumber, pageSize, term, searchBy, status);

        var itemsDto = result.Items.Select(p => new ProductResponseDto
        {
            Id = p.Id,
            Name = p.Name,
            UnitPrice = p.UnitPrice.Worth,
            Stock = p.Stock,
            IsActive = p.IsActive
        }).ToList();

        var pagedDto = new PagedResponse<ProductResponseDto>
            (itemsDto, 
            result.TotalCount, 
            result.PageNumber, 
            result.PageSize);

        return Ok(pagedDto);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ProductResponseDto>> GetById(int id)
    {
        var product = await _productsRepository.GetByIdAsync(id);

        if (product == null) return NotFound(new { message = "Producto no encontrado" });

        return Ok(new ProductResponseDto
        {
            Id = product.Id,
            Name = product.Name,
            UnitPrice = product.UnitPrice.Worth,
            Stock = product.Stock,
            IsActive = product.IsActive
        });
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")] 
    public async Task<ActionResult<ProductResponseDto>> Create(CreateProductRequest request)
    {
        try
        {
            var product = await _productService.CreateProductAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = product.Id }, new ProductResponseDto
            {
                Id = product.Id,
                Name = product.Name,
                UnitPrice = product.UnitPrice.Worth,
                Stock = product.Stock,
                IsActive = product.IsActive
            });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("EXISTING_ACTIVE"))
        {
            var productId = int.Parse(ex.Message.Split('|')[1]);
            return Conflict(new { isActive = true, productId = productId, message = "Ya existe un producto activo con este nombre." });
        }
        catch (InvalidOperationException ex) when (ex.Message.StartsWith("EXISTING_INACTIVE"))
        {
            var productId = int.Parse(ex.Message.Split('|')[1]);
            return Conflict(new { isInactive = true, productId = productId, message = "Ya existe un producto con este nombre pero está inactivo." });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrador")] 
    public async Task<IActionResult> Update(int id, UpdateProductRequest request)
    {
        if (id != request.Id) return BadRequest(new { message = "ID mismatch" });

        try
        {
            await _productService.UpdateProductAsync(request);
            return NoContent();
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("reactivate/{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Reactivate(int id)
    {
        await _productService.ReactivateProductAsync(id);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrador")] 
    public async Task<IActionResult> Delete(int id)
    {
        await _productService.DeleteProductAsync(id);
        return NoContent();
    }
}
