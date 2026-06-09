using Microsoft.JSInterop;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using Application.Features.Users;

namespace Blazor.Security;

public class JwtInterceptor : DelegatingHandler
{
    private readonly IJSRuntime _jsRuntime;
    private const string TokenKey = "authToken";
    private const string RefreshTokenKey = "refreshToken";
    private bool _isRefreshing = false;

    public JwtInterceptor(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var isLocalRequest = !request.RequestUri!.IsAbsoluteUri || request.RequestUri.Host == "localhost";

        if (isLocalRequest && !_isRefreshing && !request.RequestUri!.AbsolutePath.Contains("api/Auth/refresh"))
        {
            await CheckAndRefreshTokenAsync();
        }

        try 
        {
            var token = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenKey);
            if (!string.IsNullOrWhiteSpace(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }
        }
        catch { }

        return await base.SendAsync(request, cancellationToken);
    }

    private async Task CheckAndRefreshTokenAsync()
    {
        try
        {
            var token = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenKey);
            var refreshToken = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", RefreshTokenKey);

            if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(refreshToken)) return;

            if (IsTokenNearExpiration(token))
            {
                _isRefreshing = true;
                
                // Usamos un HttpClient limpio para evitar el interceptor y causar recursión
                using var client = new HttpClient { BaseAddress = new Uri("https://localhost:7199/") };
                var response = await client.PostAsJsonAsync("api/Auth/refresh", new TokenRequestDto(token, refreshToken));

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AuthResponseDto>();
                    if (result != null && result.IsSuccess)
                    {
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, result.Token);
                        await _jsRuntime.InvokeVoidAsync("localStorage.setItem", RefreshTokenKey, result.RefreshToken);
                    }
                }
                
                _isRefreshing = false;
            }
        }
        catch { _isRefreshing = false; }
    }

    private bool IsTokenNearExpiration(string token)
    {
        try
        {
            var parts = token.Split('.');
            if (parts.Length < 2) return false;

            var payload = parts[1];
            var jsonBytes = ParseBase64WithoutPadding(payload);
            var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

            if (keyValuePairs != null && keyValuePairs.TryGetValue("exp", out var expValue))
            {
                var expSeconds = long.Parse(expValue.ToString()!);
                var expirationTime = DateTimeOffset.FromUnixTimeSeconds(expSeconds).UtcDateTime;
                
                // Si falta menos de 5 minutos para expirar, renovamos
                return expirationTime <= DateTime.UtcNow.AddMinutes(5);
            }
        }
        catch { }
        return false;
    }

    private byte[] ParseBase64WithoutPadding(string base64)
    {
        base64 = base64.Replace('-', '+').Replace('_', '/');
        switch (base64.Length % 4)
        {
            case 2: base64 += "=="; break;
            case 3: base64 += "="; break;
        }
        return Convert.FromBase64String(base64);
    }
}
