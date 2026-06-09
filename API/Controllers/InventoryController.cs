using Application.Features.Inventory;
using Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")]
public class InventoryController : ControllerBase
{
    private readonly IInventoryService _inventoryService;
    private readonly IInventoryRepository _inventoryRepository;

    public InventoryController(IInventoryService inventoryService, IInventoryRepository inventoryRepository)
    {
        _inventoryService = inventoryService;
        _inventoryRepository = inventoryRepository;
    }

    [HttpPost("restock")]
    public async Task<IActionResult> Restock([FromBody] RestockRequest request)
    {
        try
        {
            await _inventoryService.RestockAsync(request);
            return Ok(new { message = "Reabastecimiento completado exitosamente." });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (Exception ex)
        {
            // Capturar el error real de la base de datos para diagnóstico
            var realMessage = ex.InnerException?.Message ?? ex.Message;
            return StatusCode(500, new { 
                message = "Error interno del servidor.", 
                details = realMessage,
                fullStack = ex.StackTrace 
            });
        }
    }

    [HttpGet("movements/{productId:int}")]
    public async Task<ActionResult<IEnumerable<InventoryMovementResponseDto>>> GetMovements(int productId)
    {
        var movements = await _inventoryRepository.GetProductMovementsAsync(productId);
        
        var response = movements.Select(m => new InventoryMovementResponseDto
        {
            Id = m.Id,
            ProductId = m.ProductId,
            ProductName = m.Product?.Name ?? "Producto",
            Type = m.Type,
            Quantity = m.Quantity,
            PreviousStock = m.PreviousStock,
            NewStock = m.NewStock,
            UnitCost = m.UnitCost,
            Reference = m.Reference,
            CreatedAt = m.CreatedAt,
            CreatedBy = m.CreatedBy
        });

        return Ok(response);
    }
}
