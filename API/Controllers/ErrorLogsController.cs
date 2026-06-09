using Application.Features.ErrorLogs;
using Application.Common;
using Application.Interfaces.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize(Roles = "Administrador")]
public class ErrorLogsController : ControllerBase
{
    private readonly IErrorLogRepository _errorLogRepository;

    public ErrorLogsController(IErrorLogRepository errorLogRepository)
    {
        _errorLogRepository = errorLogRepository;
    }

    [HttpGet]
    public async Task<ActionResult<PagedResponse<ErrorLogResponseDto>>> GetPaged(
        [FromQuery] int page = 1,
        [FromQuery] int size = 15,
        [FromQuery] string? searchTerm = null)
    {
        var pagedResult = await _errorLogRepository.GetPagedAsync(page, size, searchTerm);

        var dtos = pagedResult.Items.Select(e => new ErrorLogResponseDto
        {
            Id = e.Id,
            Message = e.Message,
            ExceptionType = e.ExceptionType,
            StackTrace = e.StackTrace,
            Source = e.Source,
            UserName = e.UserName,
            RequestPath = e.RequestPath,
            HttpMethod = e.HttpMethod,
            CreatedAt = e.CreatedAt
        }).ToList();

        return Ok(new PagedResponse<ErrorLogResponseDto>(dtos, pagedResult.TotalCount, page, size));
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ErrorLogResponseDto>> GetById(int id)
    {
        var e = await _errorLogRepository.GetByIdAsync(id);
        if (e == null) return NotFound();

        return Ok(new ErrorLogResponseDto
        {
            Id = e.Id,
            Message = e.Message,
            ExceptionType = e.ExceptionType,
            StackTrace = e.StackTrace,
            Source = e.Source,
            UserName = e.UserName,
            RequestPath = e.RequestPath,
            HttpMethod = e.HttpMethod,
            CreatedAt = e.CreatedAt
        });
    }

    [HttpDelete("clear-old/{days:int}")]
    public async Task<IActionResult> ClearOld(int days)
    {
        await _errorLogRepository.DeleteOldLogsAsync(days);
        return NoContent();
    }
}
