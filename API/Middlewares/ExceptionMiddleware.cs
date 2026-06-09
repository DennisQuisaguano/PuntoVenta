using System.Net;
using System.Security.Claims;
using System.Text.Json;
using Domain.Entities;
using Infrastructure.Data;

namespace API.Middlewares;

public class ExceptionMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IServiceProvider _serviceProvider;

    public ExceptionMiddleware(RequestDelegate next, IServiceProvider serviceProvider)
    {
        _next = next;
        _serviceProvider = serviceProvider;
    }

    public async Task InvokeAsync(HttpContext httpContext)
    {
        try
        {
            await _next(httpContext);
        }
        catch (Exception ex)
        {
            await HandleExceptionAsync(httpContext, ex);
        }
    }

    private async Task HandleExceptionAsync(HttpContext context, Exception exception)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;

        using (var scope = _serviceProvider.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            var userId = context.User?.FindFirstValue(ClaimTypes.NameIdentifier);
            var userName = context.User?.Identity?.Name;
            var requestPath = context.Request.Path;
            var httpMethod = context.Request.Method;

            var errorLog = new ErrorLog(
                exception.Message,
                exception.GetType().Name,
                exception.StackTrace,
                exception.Source,
                userId,
                userName,
                requestPath,
                httpMethod
            );

            try
            {
                dbContext.ErrorLogs.Add(errorLog);
                await dbContext.SaveChangesAsync();
            }
            catch
            {
                // Si falla el guardado en DB (ej: DB caída), 
                // al menos dejamos que la excepción original fluya o se maneje.
            }

            var response = new
            {
                StatusCode = context.Response.StatusCode,
                Message = "Ha ocurrido un error interno en el servidor.",
                LogId = errorLog.Id // El ID que el usuario puede dar a soporte
            };

            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            await context.Response.WriteAsync(JsonSerializer.Serialize(response, options));
        }
    }
}
