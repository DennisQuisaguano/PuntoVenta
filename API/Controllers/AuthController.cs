using Application.Features.Users;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers;

[ApiController]
[Route("api/[controller]")]
[AllowAnonymous]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login(LoginRequest request)
    {
        var result = await _authService.LoginAsync(request);

        if (!result.IsSuccess)
        {
            return Unauthorized(new { message = result.Message });
        }

        return Ok(result);
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<AuthResponseDto>> Refresh([FromBody] TokenRequestDto request)
    {
        var result = await _authService.RefreshTokenAsync(request);

        if (!result.IsSuccess)
        {
            return BadRequest(new { message = result.Message });
        }

        return Ok(result);
    }

    public class MicrosoftLoginRequest
    {
        public string Token { get; set; } = string.Empty;
    }

    [HttpPost("microsoft-login")]
    public async Task<ActionResult<AuthResponseDto>> MicrosoftLogin([FromBody] MicrosoftLoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Token)) return BadRequest("Token de Microsoft es requerido.");

        var result = await _authService.MicrosoftLoginAsync(request.Token);

        if (!result.IsSuccess)
        {
            return Unauthorized(new { message = result.Message });
        }

        return Ok(result);
    }

    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        var origin = Request.Headers["Origin"].ToString();
        var (isSuccess, message) = await _authService.ForgotPasswordAsync(request.Email, origin);

        if (!isSuccess) return BadRequest(new { message });
        return Ok(new { message });
    }

    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
    {
        var (isSuccess, message) = await _authService.ResetPasswordAsync(request);

        if (!isSuccess) return BadRequest(new { message });
        return Ok(new { message });
    }
}
