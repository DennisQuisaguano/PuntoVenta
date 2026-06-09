using System;

namespace Domain.Entities;

public class ErrorLog
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ExceptionType { get; set; }
    public string? StackTrace { get; set; }
    public string? Source { get; set; }
    
    // Información del usuario
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    
    // Contexto de la petición (Pantalla/Evento)
    public string? RequestPath { get; set; }
    public string? HttpMethod { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ErrorLog() { }

    public ErrorLog(string message, string? exceptionType, string? stackTrace, string? source, 
                    string? userId, string? userName, string? requestPath, string? httpMethod)
    {
        Message = message;
        ExceptionType = exceptionType;
        StackTrace = stackTrace;
        Source = source;
        UserId = userId;
        UserName = userName;
        RequestPath = requestPath;
        HttpMethod = httpMethod;
        CreatedAt = DateTime.UtcNow;
    }
}
