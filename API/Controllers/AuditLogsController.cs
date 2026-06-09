using Application.Common;
using Application.Features.AuditLogs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[Authorize(Roles = "Administrador")]
[ApiController]
[Route("api/[controller]")]
public class AuditLogsController : ControllerBase
{
    private readonly IAuditLogService _auditLogService;

    public AuditLogsController(IAuditLogService auditLogService)
    {
        _auditLogService = auditLogService;
    }

    [HttpGet("paged")]
    public async Task<ActionResult<PagedResponse<AuditLogResponseDto>>> GetPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 15,
        [FromQuery] string? term = null,
        [FromQuery] string searchBy = "all")
    {
        var response = await _auditLogService.GetPagedLogsAsync(pageNumber, pageSize, term, searchBy);
        return Ok(response);
    }
}
