using Application.Features.Users;
using System.Net.Http.Json;

namespace Blazor.Services;

public class AuthApiService
{
    private readonly HttpClient _http;

    public AuthApiService(HttpClient http)
    {
        _http = http;
    }

    public async Task<AuthResponseDto> LoginAsync(LoginRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/Auth/login", request);
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AuthResponseDto>() 
                       ?? new AuthResponseDto { IsSuccess = false, Message = "Respuesta vacía del servidor." };
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            try 
            {
                var errorObj = System.Text.Json.JsonDocument.Parse(errorContent);
                var msg = errorObj.RootElement.TryGetProperty("message", out var m) ? m.GetString() : errorContent;
                return new AuthResponseDto { IsSuccess = false, Message = msg };
            }
            catch 
            {
                return new AuthResponseDto { IsSuccess = false, Message = "Credenciales inválidas o error en el servidor." };
            }
        }
        catch (Exception)
        {
            return new AuthResponseDto { IsSuccess = false, Message = "Error de conexión: No se pudo comunicar con el servidor." };
        }
    }

    public async Task<AuthResponseDto> MicrosoftLoginAsync(string token)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/Auth/microsoft-login", new { token = token });
            
            if (response.IsSuccessStatusCode)
            {
                return await response.Content.ReadFromJsonAsync<AuthResponseDto>() 
                       ?? new AuthResponseDto { IsSuccess = false, Message = "Respuesta vacía del servidor." };
            }

            var errorContent = await response.Content.ReadAsStringAsync();
            try 
            {
                var errorObj = System.Text.Json.JsonDocument.Parse(errorContent);
                var msg = errorObj.RootElement.TryGetProperty("message", out var m) ? m.GetString() : errorContent;
                return new AuthResponseDto { IsSuccess = false, Message = msg };
            }
            catch 
            {
                return new AuthResponseDto { IsSuccess = false, Message = "Fallo en la autenticación de Microsoft." };
            }
        }
        catch (Exception)
        {
            return new AuthResponseDto { IsSuccess = false, Message = "Error de conexión: No se pudo comunicar con el servidor." };
        }
    }

    public async Task<AuthResponseDto> ForgotPasswordAsync(ForgotPasswordRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/Auth/forgot-password", request);
            if (response.IsSuccessStatusCode)
            {
                return new AuthResponseDto { IsSuccess = true, Message = "Enlace enviado. Revise su correo." };
            }

            var error = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            return error ?? new AuthResponseDto { IsSuccess = false, Message = "Error al procesar la solicitud." };
        }
        catch (Exception)
        {
            return new AuthResponseDto { IsSuccess = false, Message = "Error de conexión." };
        }
    }

    public async Task<AuthResponseDto> ResetPasswordAsync(ResetPasswordRequest request)
    {
        try
        {
            var response = await _http.PostAsJsonAsync("api/Auth/reset-password", request);
            if (response.IsSuccessStatusCode)
            {
                return new AuthResponseDto { IsSuccess = true, Message = "Contraseña restablecida correctamente." };
            }

            var error = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
            return error ?? new AuthResponseDto { IsSuccess = false, Message = "Error al restablecer la contraseña." };
        }
        catch (Exception)
        {
            return new AuthResponseDto { IsSuccess = false, Message = "Error de conexión." };
        }
    }
}
