using System;

namespace Domain.Entities;

public class AuditLog
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // Create, Update, Delete
    public string TableName { get; set; } = string.Empty;
    public DateTime DateTime { get; set; }
    public string? OldValues { get; set; } // JSON format
    public string? NewValues { get; set; } // JSON format
    public string? AffectedColumns { get; set; } // JSON format
    public string PrimaryKey { get; set; } = string.Empty;
}
