namespace Domain.Common;

public interface IAuditable
{
    bool IsActive { get; }
    DateTime CreatedAt { get; set; }
    string? CreatedBy { get; set; }
    DateTime? UpdatedAt { get; set; }
    string? UpdatedBy { get; set; }
    DateTime? DeletedAt { get; set; }
    string? DeletedBy { get; set; }

    void Activate();
    void Deactivate(string? deletedBy = null);
}
