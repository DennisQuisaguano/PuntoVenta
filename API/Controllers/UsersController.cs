using Application.Common;
using Application.Features.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize] 
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;

    public UsersController(IUserService userService)
    {
        _userService = userService;
    }

    [HttpGet]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<IEnumerable<UserResponseDto>>> GetAll()
    {
        var users = await _userService.GetAllUsersAsync();
        return Ok(users);
    }

    [HttpGet("paged")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<PagedResponse<UserResponseDto>>> GetPaged(
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? term = null,
        [FromQuery] string searchBy = "name",
        [FromQuery] string status = "active")
    {
        var (items, totalCount) = await _userService.GetPagedUsersAsync(pageNumber, pageSize, term, searchBy, status);
        return Ok(new PagedResponse<UserResponseDto>(items.ToList(), totalCount, pageNumber, pageSize));
    }

    [HttpGet("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<UserResponseDto>> GetById(int id)
    {
        var user = await _userService.GetUserByIdAsync(id);
        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpPost]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Create(RegisterUserRequest request)
    {
        var result = await _userService.CreateUserAsync(request);
        
        if (result.IsSuccess) return Ok(new { message = "Usuario creado exitosamente" });

        var firstError = result.Errors.FirstOrDefault();
        if (firstError != null && firstError.StartsWith("INACTIVE_USER_EXISTS"))
        {
            var userId = int.Parse(firstError.Split('|')[1]);
            return Conflict(new { isInactive = true, userId = userId, message = "El usuario existe pero está inactivo." });
        }

        return BadRequest(new { message = string.Join("\n", result.Errors) });
    }

    [HttpPut("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Update(int id, UpdateUserRequest request)
    {
        if (id != request.Id) return BadRequest(new { message = "ID mismatch" });
        
        var result = await _userService.UpdateUserAsync(request);
        if (result.IsSuccess) return NoContent();
        return BadRequest(new { message = string.Join("\n", result.Errors) });
    }

    [HttpPut("reactivate/{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Reactivate(int id, UpdateUserRequest request)
    {
        var result = await _userService.ReactivateUserAsync(id, request);
        if (result.IsSuccess) return Ok(new { message = "Usuario reactivado exitosamente" });
        return BadRequest(new { message = string.Join("\n", result.Errors) });
    }

    [HttpPut("unlock/{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Unlock(int id)
    {
        var result = await _userService.UnlockUserAsync(id);
        if (result) return Ok(new { message = "Usuario desbloqueado exitosamente" });
        return BadRequest(new { message = "No se pudo desbloquear al usuario." });
    }

    [HttpDelete("{id:int}")]
    [Authorize(Roles = "Administrador")]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _userService.DeleteUserAsync(id);
        if (result) return NoContent();
        return BadRequest(new { message = "No se pudo eliminar el usuario." });
    }

    [HttpGet("profile")]
    public async Task<ActionResult<UserResponseDto>> GetProfile()
    {
        var userName = User.Identity?.Name;
        if (string.IsNullOrEmpty(userName)) return Unauthorized();

        var user = await _userService.GetUserByUserNameAsync(userName);
        if (user == null) return NotFound();
        return Ok(user);
    }

    [HttpGet("profile-stats")]
    public async Task<ActionResult<ProfileStatsDto>> GetProfileStats()
    {
        var userName = User.Identity?.Name;
        var role = User.FindFirstValue(System.Security.Claims.ClaimTypes.Role) ?? "Vendedor";
        
        if (string.IsNullOrEmpty(userName)) return Unauthorized();

        var stats = await _userService.GetProfileStatsAsync(userName, role);
        return Ok(stats);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile(UpdateUserRequest request)
    {
        var userName = User.Identity?.Name;
        if (string.IsNullOrEmpty(userName)) return Unauthorized();

        var user = await _userService.GetUserByUserNameAsync(userName);
        if (user == null || user.Id != request.Id) 
            return BadRequest(new { message = "Solicitud inválida" });

        var result = await _userService.UpdateUserAsync(request);
        if (result.IsSuccess) return NoContent();
        return BadRequest(new { message = string.Join("\n", result.Errors) });
    }

    [HttpPut("change-password")]
    public async Task<IActionResult> ChangePassword(ChangePasswordRequest request)
    {
        var userName = User.Identity?.Name;
        if (string.IsNullOrEmpty(userName)) return Unauthorized();

        if (request.NewPassword != request.ConfirmPassword)
            return BadRequest(new { message = "La nueva contraseña y la confirmación no coinciden." });

        var user = await _userService.GetUserByUserNameAsync(userName);
        if (user == null) return NotFound();

        var result = await _userService.ChangePasswordAsync(user.Id, request.CurrentPassword, request.NewPassword);
        if (result.IsSuccess) return Ok(new { message = "Contraseña actualizada exitosamente" });
        return BadRequest(new { message = string.Join("\n", result.Errors) });
    }

    [HttpGet("roles")]
    [Authorize(Roles = "Administrador")]
    public async Task<ActionResult<IEnumerable<string>>> GetRoles()
    {
        var roles = await _userService.GetRolesAsync();
        return Ok(roles);
    }
}
