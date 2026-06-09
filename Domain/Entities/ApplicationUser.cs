using Domain.Common;
using Microsoft.AspNetCore.Identity;

namespace Domain.Entities;

public class ApplicationUser : IdentityUser<int>, IAuditable
{
    private string _firstName = string.Empty;
    public string FirstName 
    { 
        get => _firstName; 
        set => _firstName = value?.Trim().ToUpper() ?? string.Empty; 
    }

    private string _lastName = string.Empty;
    public string LastName 
    { 
        get => _lastName; 
        set => _lastName = value?.Trim().ToUpper() ?? string.Empty; 
    }

    private string? _idCard;
    public string? IDCard 
    { 
        get => _idCard; 
        set => _idCard = value?.Trim().ToUpper(); 
    }

    private string? _address;
    public string? Address 
    { 
        get => _address; 
        set => _address = value?.Trim().ToUpper(); 
    }

    public override string? UserName 
    { 
        get => base.UserName; 
        set => base.UserName = value?.Trim().ToUpper(); 
    }

    public override string? Email 
    { 
        get => base.Email; 
        set 
        {
            if (!string.IsNullOrEmpty(value) && !IsValidEmail(value))
                throw new ArgumentException("El formato del correo electrónico no es válido.");
            base.Email = value?.Trim();
        }
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address.Equals(email, StringComparison.OrdinalIgnoreCase);
        }
        catch { return false; }
    }

    public DateTime? LastLogin { get; set; }
    public bool MustChangePassword { get; set; } = false;

    // Refresh Token para seguridad JWT
    public string? RefreshToken { get; set; }
    public DateTime RefreshTokenExpiryTime { get; set; }
    
    // Propiedades de IAuditable
    public bool IsActive { get;  set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string? CreatedBy { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public DateTime? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }

    public void Activate()
    {
        IsActive = true;
        DeletedAt = null;
        DeletedBy = null;
    }

    public void Deactivate(string? deletedBy = null)
    {
        IsActive = false;
        DeletedAt = DateTime.UtcNow;
        DeletedBy = deletedBy;
    }
}
