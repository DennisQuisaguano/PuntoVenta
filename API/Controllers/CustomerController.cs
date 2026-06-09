using Application.Common;
using Application.Exceptions;
using Application.Interfaces.Repositories;
using Application.Features.Customers;
using Domain.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class CustomerController : ControllerBase
{
    private readonly ICustomerRepository _customerRepository;
    private readonly ICustomerService _customerService;

    public CustomerController(ICustomerRepository customerRepository, ICustomerService customerService)
    {
        _customerRepository = customerRepository;
        _customerService = customerService;
    }

    [HttpGet("search")]
    public async Task<ActionResult<PagedResponse<CustomerResponseDto>>> Search(
        [FromQuery] string? term = null,
        [FromQuery] string criterion = "card",
        [FromQuery] int page = 1,
        [FromQuery] int size = 10,
        [FromQuery] string status = "active")
    {
        // Seguridad: Solo admin puede ver registros inactivos o todos
        if (status != "active" && !User.IsInRole("Administrador"))
        {
            status = "active";
        }

        var result = await _customerRepository.GetPagedAsync(page, size, term, criterion, status);

        var itemsDto = result.Items.Select(c => new CustomerResponseDto
        {
            Id = c.Id,
            IDCard = c.IDCard,
            Name = c.Name,
            LastName = c.LastName,
            Phone = c.Phone,
            Address = c.Address,
            Email = c.Email,
            IsActive = c.IsActive
        }).ToList();

        return Ok(new PagedResponse<CustomerResponseDto>(itemsDto, result.TotalCount, page, size));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerResponseDto>> GetById(int id)
    {
        var customer = await _customerRepository.GetByIdAsync(id);

        if (customer == null) return NotFound(new { message = "Cliente no encontrado" });

        return Ok(new CustomerResponseDto
        {
            Id = customer.Id,
            IDCard = customer.IDCard,
            Name = customer.Name,
            LastName = customer.LastName,
            Phone = customer.Phone,
            Address = customer.Address,
            Email = customer.Email,
            IsActive = customer.IsActive
        });
    }

    [HttpPost]
    // Tanto Vendedor como Admin pueden crear clientes
    public async Task<ActionResult<CustomerResponseDto>> Create(CreateCustomerRequest request)
    {
        try
        {
            var customer = await _customerService.CreateCustomerAsync(request);
            return CreatedAtAction(nameof(GetById), new { id = customer.Id }, new CustomerResponseDto
            {
                Id = customer.Id,
                IDCard = customer.IDCard,
                Name = customer.Name,
                LastName = customer.LastName,
                Phone = customer.Phone,
                Address = customer.Address,
                Email = customer.Email,
                IsActive = customer.IsActive
            });
        }
        catch (InactiveCustomerExistsException ex)
        {
            return Conflict(new { message = ex.Message, isInactive = true, customerId = ex.CustomerId });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrador")] // Solo el administrador edita clientes existentes
    public async Task<IActionResult> Update(int id, UpdateCustomerRequest request)
    {
        if (id != request.Id) return BadRequest(new { message = "ID mismatch" });

        try
        {
            await _customerService.UpdateCustomerAsync(request);
            return NoContent();
        }
        catch (InactiveCustomerExistsException ex)
        {
            return Conflict(new { message = ex.Message, isInactive = true, customerId = ex.CustomerId });
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new { message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpPut("reactivate/{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Reactivate(int id, UpdateCustomerRequest request)
    {
        if (id != request.Id) return BadRequest(new { message = "ID mismatch" });

        try
        {
            await _customerService.ReactivateCustomerAsync(id, request);
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
        catch (InvalidOperationException ex)
        {
            return Conflict(new { message = ex.Message });
        }
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrador")] // Solo el administrador inactiva clientes
    public async Task<IActionResult> Delete(int id)
    {
        await _customerService.DeleteCustomerAsync(id);
        return NoContent();
    }
}
