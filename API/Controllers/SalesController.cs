using Application.Common;
using Application.Features.Sales;
using Domain.Exceptions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SalesController : ControllerBase
{
    private readonly ISaleCommandService _saleCommandService;
    private readonly ISaleQueryService _saleQueryService;

    public SalesController(
        ISaleCommandService saleCommandService, 
        ISaleQueryService saleQueryService)
    {
        _saleCommandService = saleCommandService;
        _saleQueryService = saleQueryService;
    }

    [HttpGet("search")]
    public async Task<ActionResult<IEnumerable<SaleResponseDto>>> SearchSales(
        [FromQuery] string term, 
        [FromQuery] string searchBy = "number",
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        string? sellerNameFilter = User.IsInRole("Administrador") ? null : User.Identity?.Name;

        var sales = await _saleQueryService.ExecuteAsync(term, searchBy, sellerNameFilter, startDate, endDate);

        var responseItems = sales.Select(s => MapToDto(s));

        return Ok(responseItems);
    }

    [HttpPost]
    [Authorize(Roles = "Vendedor")]
    public async Task<ActionResult<SaleResponseDto>> RegisterSale([FromBody] CreateSaleRequest request)
    {
        if (request == null || !request.Details.Any())
            return BadRequest("The sale request is empty or invalid.");

        try
        {
            var result = await _saleCommandService.CreateSaleAsync(request);
            var response = MapToDto(result);

            return CreatedAtAction(nameof(RegisterSale), new { id = response.Id }, response);
        }
        catch (ProductDeletedException ex)
        {
            return BadRequest(new { type = "ProductDeleted", message = ex.Message, deletedProducts = ex.DeletedProducts });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "An unexpected error occurred.", details = ex.Message });
        }
    }

    [HttpPut("{id:int}/confirm")]
    public async Task<IActionResult> ConfirmSale(int id)
    {
        try
        {
            await _saleCommandService.ConfirmSaleAsync(id);
            return Ok(new { message = "Venta confirmada y stock actualizado." });
        }
        catch (BulkStockException ex)
        {
            return Conflict(new { type = "BulkStock", message = ex.Message, errors = ex.Errors });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}/cancel")]
    public async Task<IActionResult> CancelSale(int id)
    {
        try
        {
            await _saleCommandService.CancelSaleAsync(id);
            return Ok(new { message = "Venta anulada correctamente." });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
    }

    [HttpGet("paged")]
    public async Task<ActionResult<PagedResponse<SaleResponseDto>>> GetPagedSales(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? term = null,
        [FromQuery] string? searchBy = null,
        [FromQuery] string? status = null,
        [FromQuery] DateTime? startDate = null,
        [FromQuery] DateTime? endDate = null)
    {
        string? sellerNameFilter = User.IsInRole("Administrador") ? null : User.Identity?.Name;

        var (sales, totalCount) = await _saleQueryService.ExecutePagedAsync(pageNumber, pageSize, term, searchBy, sellerNameFilter, startDate, endDate, status);

        var responseItems = sales.Select(s => MapToDto(s));

        return Ok(new PagedResponse<SaleResponseDto>(responseItems.ToList(), totalCount, pageNumber, pageSize));
    }

    private static SaleResponseDto MapToDto(Domain.Entities.Sale s)
    {
        return new SaleResponseDto
        {
            Id = s.Id,
            InvoiceNumber = s.InvoiceNumber,
            IssueDate = s.IssueDate,
            CustomerName = $"{s.CustomerName} {s.CustomerLastName}",
            CustomerIDCard = s.CustomerIDCard,
            CustomerPhone = s.CustomerPhone,
            CustomerAddress = s.CustomerAddress,
            CustomerEmail = s.CustomerEmail,
            CreatedBy = s.CreatedBy ?? "SYSTEM",
            SellerName = s.SellerName,
            SellerLastName = s.SellerLastName,
            SubTotal = s.SubTotal,
            VatAmount = s.VatAmount,
            VatPercentage = s.VatPercentage,
            Total = s.Total,
            Status = s.Status,
            Details = s.Details.Select(d => new SaleDetailResponseDto
            {
                ProductId = d.ProductId,
                ProductName = d.ProductName, // Snapshot del nombre del producto
                Amount = d.Amount,
                UnitPrice = d.UnitPrice.Worth,
                SubTotal = d.SubTotal.Worth
            }).ToList()
        };
    }
}
