namespace Application.Features.ErrorLogs;

public class ErrorLogResponseDto
{
    public int Id { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? ExceptionType { get; set; }
    public string? StackTrace { get; set; }
    public string? Source { get; set; }
    public string? UserName { get; set; }
    public string? RequestPath { get; set; }
    public string? HttpMethod { get; set; }
    public DateTime CreatedAt { get; set; }
}
