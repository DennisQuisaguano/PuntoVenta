using Blazor;
using Blazor.Security;
using Blazor.Services;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// --- AUTENTICACIÓN MICROSOFT (MSAL) ---
builder.Services.AddMsalAuthentication(options =>
{
    builder.Configuration.Bind("AzureAd", options.ProviderOptions.Authentication);
    options.ProviderOptions.DefaultAccessTokenScopes.Add("openid");
    options.ProviderOptions.DefaultAccessTokenScopes.Add("profile");
    options.ProviderOptions.DefaultAccessTokenScopes.Add("email");
});

// --- REPARACIÓN DE INYECCIÓN DE DEPENDENCIAS PARA MSAL + JWT PROPIO ---
builder.Services.AddScoped<Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticationService<Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticationState, Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteUserAccount, Microsoft.Authentication.WebAssembly.Msal.Models.MsalProviderOptions>>();
builder.Services.AddScoped<Microsoft.AspNetCore.Components.WebAssembly.Authentication.IRemoteAuthenticationService<Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticationState>>(sp => sp.GetRequiredService<Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticationService<Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticationState, Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteUserAccount, Microsoft.Authentication.WebAssembly.Msal.Models.MsalProviderOptions>>());
builder.Services.AddScoped<Microsoft.AspNetCore.Components.WebAssembly.Authentication.IAccessTokenProvider>(sp => sp.GetRequiredService<Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticationService<Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteAuthenticationState, Microsoft.AspNetCore.Components.WebAssembly.Authentication.RemoteUserAccount, Microsoft.Authentication.WebAssembly.Msal.Models.MsalProviderOptions>>());

// --- SEGURIDAD Y JWT ---
builder.Services.AddAuthorizationCore();
builder.Services.AddScoped<CustomAuthStateProvider>();
builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<CustomAuthStateProvider>());
builder.Services.AddTransient<JwtInterceptor>();

// --- CONFIGURACIÓN DE HTTPCLIENT ---
// Configurado para conectar con la API en modo HTTPS (Puerto 7199)
builder.Services.AddScoped(sp => 
{
    var interceptor = sp.GetRequiredService<JwtInterceptor>();
    interceptor.InnerHandler = new HttpClientHandler();
    return new HttpClient(interceptor) { BaseAddress = new Uri("https://localhost:7199/") };
});

builder.Services.AddScoped<AuthApiService>();
builder.Services.AddScoped<UserApiService>();
builder.Services.AddScoped<PosApiService>();
builder.Services.AddScoped<CustomerApiService>();
builder.Services.AddScoped<NotificationApiService>();
builder.Services.AddScoped<ErrorLogApiService>();
builder.Services.AddScoped<AuditLogApiService>();
builder.Services.AddScoped<ShortcutService>();

await builder.Build().RunAsync();
