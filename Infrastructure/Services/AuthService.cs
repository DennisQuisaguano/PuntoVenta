using Application.Features.Users;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.IdentityModel.Protocols;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Security.Cryptography;

namespace Infrastructure.Services;

public class AuthService : IAuthService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IConfiguration _configuration;
    private readonly IEmailService _emailService; 

    public AuthService(UserManager<ApplicationUser> userManager, IConfiguration configuration, IEmailService emailService)
    {
        _userManager = userManager;
        _configuration = configuration;
        _emailService = emailService; 
    }

    public async Task<(bool IsSuccess, string Message)> ForgotPasswordAsync(string email, string origin)
    {
        var user = await _userManager.FindByEmailAsync(email);
        
        if (user == null || !user.IsActive)
            return (true, "Si el correo está registrado, recibirá instrucciones en breve.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        
        var encodedToken = System.Web.HttpUtility.UrlEncode(token);
        var resetUrl = $"{origin}/reset-password?token={encodedToken}&email={email}";

        var body = $@"
            <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #eee; padding: 20px; border-radius: 10px;'>
                <h2 style='color: #6B1A33; text-align: center;'>Recuperación de Contraseña</h2>
                <p>Hola <strong>{user.FirstName}</strong>,</p>
                <p>Has solicitado restablecer tu contraseña en el <strong>Sistema Punto Venta</strong>. Haz clic en el botón de abajo para continuar:</p>
                <div style='text-align: center; margin: 30px 0;'>
                    <a href='{resetUrl}' style='background-color: #6B1A33; color: white; padding: 12px 25px; text-decoration: none; border-radius: 5px; font-weight: bold;'>Restablecer mi Contraseña</a>
                </div>
                <p style='font-size: 0.8rem; color: #777;'>Este enlace expirará en 24 horas. Si no solicitaste este cambio, puedes ignorar este correo.</p>
                <hr style='border: 0; border-top: 1px solid #eee; margin: 20px 0;'>
                <p style='text-align: center; font-size: 0.7rem; color: #999;'>© 2026 Sistema UTA POS - Todos los derechos reservados.</p>
            </div>";

        try
        {
            await _emailService.SendEmailAsync(email, "Recuperación de Contraseña - UTA POS", body);
            return (true, "Si el correo está registrado, recibirá instrucciones en breve.");
        }
        catch (Exception ex)
        {
            return (false, "Error al enviar el correo: " + ex.Message);
        }
    }

    public async Task<(bool IsSuccess, string Message)> ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await _userManager.FindByEmailAsync(request.Email);
        if (user == null) return (false, "Solicitud inválida.");

        if (request.NewPassword != request.ConfirmPassword)
            return (false, "Las contraseñas no coinciden.");

        var result = await _userManager.ResetPasswordAsync(user, request.Token, request.NewPassword);
        
        if (result.Succeeded)
        {
            user.MustChangePassword = false; 
            await _userManager.UpdateAsync(user);
            return (true, "Contraseña restablecida con éxito. Ya puedes iniciar sesión.");
        }

        return (false, "El enlace ha expirado o es inválido. Por favor, solicita uno nuevo.");
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequest request)
    {
        var user = await _userManager.FindByNameAsync(request.UserName);

        if (user == null || !user.IsActive)
        {
            return new AuthResponseDto { IsSuccess = false, Message = "Usuario no encontrado o inactivo." };
        }

        if (await _userManager.IsLockedOutAsync(user))
        {
            var lockoutEnd = await _userManager.GetLockoutEndDateAsync(user);
            var timeLeft = lockoutEnd.Value - DateTimeOffset.UtcNow;

            if (timeLeft.TotalDays > 365)
            {
                 return new AuthResponseDto { IsSuccess = false, Message = "Tu cuenta ha sido bloqueada permanentemente por seguridad. Contacta al administrador." };
            }

            return new AuthResponseDto { IsSuccess = false, Message = $"Demasiados intentos. Intente de nuevo en {Math.Ceiling(timeLeft.TotalMinutes)} minutos." };
        }

        var isPasswordValid = await _userManager.CheckPasswordAsync(user, request.Password);
        var isAdmin = await _userManager.IsInRoleAsync(user, "Administrador");

        if (!isPasswordValid)
        {
            await _userManager.AccessFailedAsync(user);
            var failedCount = await _userManager.GetAccessFailedCountAsync(user);

            if (isAdmin)
            {
                if (failedCount <= 3) return new AuthResponseDto { IsSuccess = false, Message = "Contraseña incorrecta." };
                else if (failedCount == 4)
                {
                    await Task.Delay(10000);
                    return new AuthResponseDto { IsSuccess = false, Message = "Contraseña incorrecta. Reintento disponible en 10 segundos." };
                }
                else
                {
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddMinutes(15));
                    await _userManager.ResetAccessFailedCountAsync(user);
                    return new AuthResponseDto { IsSuccess = false, Message = "Seguridad: Cuenta suspendida por 15 minutos debido a actividad sospechosa." };
                }
            }
            else
            {
                var remaining = 3 - failedCount;
                if (remaining > 0) return new AuthResponseDto { IsSuccess = false, Message = $"Contraseña incorrecta. Le quedan {remaining} intentos antes de bloquear su cuenta." };
                else
                {
                    await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.UtcNow.AddYears(100));
                    return new AuthResponseDto { IsSuccess = false, Message = "Ha superado el límite de intentos. Su cuenta ha sido bloqueada. Contacte al administrador." };
                }
            }
        }

        await _userManager.ResetAccessFailedCountAsync(user);

        var roles = await _userManager.GetRolesAsync(user);
        var token = GenerateJwtToken(user, roles);

        var refreshToken = GenerateRefreshToken();
        user.RefreshToken = refreshToken;
        user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); 

        user.LastLogin = DateTime.UtcNow;
        await _userManager.UpdateAsync(user);

        return new AuthResponseDto
        {
            IsSuccess = true,
            Token = token,
            RefreshToken = refreshToken,
            UserName = user.UserName,
            Role = roles.FirstOrDefault(),
            Expiration = DateTime.UtcNow.AddMinutes(480) 
        };
    }

    public async Task<AuthResponseDto> RefreshTokenAsync(TokenRequestDto request)
    {
        var principal = GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal == null) return new AuthResponseDto { IsSuccess = false, Message = "Token de acceso inválido." };

        var userName = principal.Identity?.Name;
        var user = await _userManager.FindByNameAsync(userName!);

        if (user == null || user.RefreshToken != request.RefreshToken || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
        {
            return new AuthResponseDto { IsSuccess = false, Message = "Sesión expirada o Refresh Token inválido." };
        }

        var roles = await _userManager.GetRolesAsync(user);
        var newAccessToken = GenerateJwtToken(user, roles);
        var newRefreshToken = GenerateRefreshToken();

        user.RefreshToken = newRefreshToken;
        await _userManager.UpdateAsync(user);

        return new AuthResponseDto
        {
            IsSuccess = true,
            Token = newAccessToken,
            RefreshToken = newRefreshToken,
            UserName = user.UserName,
            Role = roles.FirstOrDefault(),
            Expiration = DateTime.UtcNow.AddMinutes(480)
        };
    }

    private string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]!)),
            ValidateLifetime = false 
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);

        if (securityToken is not JwtSecurityToken jwtSecurityToken || 
            !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
        {
            return null;
        }

        return principal;
    }

    private string GenerateJwtToken(ApplicationUser user, IList<string> roles)
    {
        var authClaims = new List<Claim>
        {
            new Claim(ClaimTypes.Name, user.UserName!),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("FirstName", user.FirstName),
            new Claim("LastName", user.LastName),
            new Claim("MustChangePassword", user.MustChangePassword.ToString().ToLower())
        };

        foreach (var role in roles)
        {
            authClaims.Add(new Claim(ClaimTypes.Role, role));
        }

        var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]!));

        var token = new JwtSecurityToken(
            issuer: _configuration["JWT:ValidIssuer"],
            audience: _configuration["JWT:ValidAudience"],
            expires: DateTime.UtcNow.AddMinutes(480), 
            claims: authClaims,
            signingCredentials: new SigningCredentials(authSigningKey, SecurityAlgorithms.HmacSha256)
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public async Task<AuthResponseDto> MicrosoftLoginAsync(string microsoftToken)
    {
        try
        {
            var tenantId = _configuration["AzureAd:TenantId"];
            var clientId = _configuration["AzureAd:ClientId"];

            if (string.IsNullOrEmpty(tenantId) || string.IsNullOrEmpty(clientId))
                return new AuthResponseDto { IsSuccess = false, Message = "Configuración de AzureAD incompleta." };

            var stsDiscoveryEndpoint = $"https://login.microsoftonline.com/{tenantId}/v2.0/.well-known/openid-configuration";
            var configManager = new ConfigurationManager<OpenIdConnectConfiguration>(stsDiscoveryEndpoint, new OpenIdConnectConfigurationRetriever());
            var config = await configManager.GetConfigurationAsync();

            var handler = new JwtSecurityTokenHandler();
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = $"https://login.microsoftonline.com/{tenantId}/v2.0",
                ValidateAudience = true,
                ValidAudience = clientId,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKeys = config.SigningKeys
            };

            var principal = handler.ValidateToken(microsoftToken, validationParameters, out _);
            var email = principal.FindFirst("email")?.Value ?? principal.FindFirst(ClaimTypes.Email)?.Value ?? principal.FindFirst("preferred_username")?.Value;

            if (string.IsNullOrEmpty(email)) return new AuthResponseDto { IsSuccess = false, Message = "No se encontró un email asociado." };

            var user = await _userManager.FindByEmailAsync(email);

            if (user == null)
            {
                var firstName = principal.FindFirst("given_name")?.Value ?? principal.FindFirst(ClaimTypes.GivenName)?.Value ?? "USUARIO";
                var lastName = principal.FindFirst("family_name")?.Value ?? principal.FindFirst(ClaimTypes.Surname)?.Value ?? "MICROSOFT";

                user = new ApplicationUser { UserName = email, Email = email, FirstName = firstName.ToUpper(), LastName = lastName.ToUpper(), IsActive = true };
                var result = await _userManager.CreateAsync(user, Guid.NewGuid().ToString("N").Substring(0, 8) + "Ab1!@");
                if (result.Succeeded) await _userManager.AddToRoleAsync(user, "Vendedor");
            }

            if (!user.IsActive) return new AuthResponseDto { IsSuccess = false, Message = "Cuenta desactivada." };

            var roles = await _userManager.GetRolesAsync(user);
            var ourToken = GenerateJwtToken(user, roles);

            var refreshToken = GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(5);

            user.LastLogin = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);

            return new AuthResponseDto
            {
                IsSuccess = true,
                Token = ourToken,
                RefreshToken = refreshToken,
                UserName = user.UserName,
                Role = roles.FirstOrDefault(),
                Expiration = DateTime.UtcNow.AddMinutes(480)
            };
        }
        catch (Exception ex)
        {
            return new AuthResponseDto { IsSuccess = false, Message = "Fallo en la autenticación externa: " + ex.Message };
        }
    }
}
