using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.JSInterop;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text.Json;

namespace Blazor.Security;

public class CustomAuthStateProvider : AuthenticationStateProvider
{
    private readonly IJSRuntime _jsRuntime;
    private readonly HttpClient _httpClient;
    private const string TokenKey = "authToken";

    public CustomAuthStateProvider(IJSRuntime jsRuntime, HttpClient httpClient)
    {
        _jsRuntime = jsRuntime;
        _httpClient = httpClient;
    }

    public override async Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        try
        {
            var token = await _jsRuntime.InvokeAsync<string?>("localStorage.getItem", TokenKey);

            if (string.IsNullOrWhiteSpace(token) || token == "undefined" || token == "null")
            {
                return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
            }

            var claims = ParseClaimsFromJwt(token).ToList();

            // Validar la expiración del token
            var expClaim = claims.FirstOrDefault(c => c.Type == "exp");
            if (expClaim != null && long.TryParse(expClaim.Value, out var exp))
            {
                var expirationTime = DateTimeOffset.FromUnixTimeSeconds(exp).UtcDateTime;
                if (expirationTime <= DateTime.UtcNow)
                {
                    // El token ha expirado, lo limpiamos de la sesión actual (sin romper el flujo)
                    await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
                    return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
                }
            }
            
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt", ClaimTypes.Name, ClaimTypes.Role)));
        }
        catch
        {
            // Fallback seguro en caso de token corrupto
            await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
            return new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity()));
        }
    }

    public async Task NotifyUserAuthentication(string token, string? forceRole = null)
    {
        try 
        {
            await _jsRuntime.InvokeVoidAsync("localStorage.setItem", TokenKey, token);

            var claims = ParseClaimsFromJwt(token).ToList();

            if (!string.IsNullOrEmpty(forceRole))
            {
                // Remover cualquier rol previo para priorizar el forzado
                claims.RemoveAll(c => c.Type == ClaimTypes.Role || c.Type == "role");
                claims.Add(new Claim(ClaimTypes.Role, forceRole));
            }

            var authenticatedUser = new ClaimsPrincipal(new ClaimsIdentity(claims, "jwt", ClaimTypes.Name, ClaimTypes.Role));
            var authState = Task.FromResult(new AuthenticationState(authenticatedUser));
            NotifyAuthenticationStateChanged(authState);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in NotifyUserAuthentication: {ex.Message}");
        }
    }

    public async Task NotifyUserLogout()
    {
        await _jsRuntime.InvokeVoidAsync("localStorage.removeItem", TokenKey);
        var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
        var authState = Task.FromResult(new AuthenticationState(anonymousUser));
        NotifyAuthenticationStateChanged(authState);
    }

    private IEnumerable<Claim> ParseClaimsFromJwt(string jwt)
    {
        var parts = jwt.Split('.');
        if (parts.Length < 2) return Enumerable.Empty<Claim>();

        var payload = parts[1];
        var jsonBytes = ParseBase64WithoutPadding(payload);
        var keyValuePairs = JsonSerializer.Deserialize<Dictionary<string, object>>(jsonBytes);

        if (keyValuePairs == null) return Enumerable.Empty<Claim>();

        var claims = new List<Claim>();

        // 1. Extraer Roles (pueden venir como "role" o como el URI largo)
        if (keyValuePairs.TryGetValue(ClaimTypes.Role, out object? roleClaims) || 
            keyValuePairs.TryGetValue("role", out roleClaims))
        {
            if (roleClaims != null)
            {
                var roleStr = roleClaims.ToString();
                if (roleStr!.Trim().StartsWith("["))
                {
                    var roles = JsonSerializer.Deserialize<string[]>(roleStr);
                    foreach (var role in roles!) claims.Add(new Claim(ClaimTypes.Role, role));
                }
                else
                {
                    claims.Add(new Claim(ClaimTypes.Role, roleStr));
                }
            }
            keyValuePairs.Remove(ClaimTypes.Role);
            keyValuePairs.Remove("role");
        }

        // 2. Extraer Nombre de Usuario
        if (keyValuePairs.TryGetValue(ClaimTypes.Name, out object? nameClaim) || 
            keyValuePairs.TryGetValue("unique_name", out nameClaim))
        {
            if (nameClaim != null) claims.Add(new Claim(ClaimTypes.Name, nameClaim.ToString()!));
            keyValuePairs.Remove(ClaimTypes.Name);
            keyValuePairs.Remove("unique_name");
        }

        // 3. Extraer ID de Usuario
        if (keyValuePairs.TryGetValue(ClaimTypes.NameIdentifier, out object? nameIdClaim) || 
            keyValuePairs.TryGetValue("nameid", out nameIdClaim))
        {
            if (nameIdClaim != null) claims.Add(new Claim(ClaimTypes.NameIdentifier, nameIdClaim.ToString()!));
            keyValuePairs.Remove(ClaimTypes.NameIdentifier);
            keyValuePairs.Remove("nameid");
        }

        // 4. Agregar el resto de claims
        claims.AddRange(keyValuePairs.Select(kvp => new Claim(kvp.Key, kvp.Value.ToString() ?? "")));

        return claims;
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
