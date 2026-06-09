using Domain.Common;

namespace Domain.Entities;

public class Customer : AuditableEntity
{
    public int Id { get; set; }
    public string IDCard { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string LastName { get; private set; } = string.Empty;
    public string Phone { get; private set; } = string.Empty;
    public string Address { get; private set; } = string.Empty;
    public string Email { get; private set; } = string.Empty;

    protected Customer() { }

    public Customer(string idCard, string name, string lastName, string phone, string address, string email)
    {
        Update(idCard, name, lastName, phone, address, email);
    }

    public void Update(string idCard, string name, string lastName, string phone, string address, string email)
    {
        if (string.IsNullOrWhiteSpace(idCard))
            throw new ArgumentException("La cédula es requerida.");

        string sanitizedIdCard = idCard.Trim().Replace(" ", "").ToUpper();
        if (sanitizedIdCard.Length != 10 || !sanitizedIdCard.All(char.IsDigit))
            throw new ArgumentException("La cédula debe tener exactamente 10 dígitos numéricos.");

        string sanitizedPhone = (phone ?? "").Trim().Replace(" ", "").ToUpper();
        if (sanitizedPhone.Length != 10 || !sanitizedPhone.All(char.IsDigit) || !sanitizedPhone.StartsWith("09"))
            throw new ArgumentException("El teléfono debe tener 10 dígitos numéricos y empezar con '09'.");

        if (!IsValidEmail(email))
            throw new ArgumentException("El formato del email no es válido.");

        // New validations for Name and LastName
        ValidateNamePart(name, "Nombre");
        ValidateNamePart(lastName, "Apellido");

        IDCard = sanitizedIdCard;
        Name = name.Trim().ToUpper();
        LastName = lastName.Trim().ToUpper();
        Phone = sanitizedPhone;
        Address = address?.Trim().ToUpper() ?? "";
        Email = email?.Trim() ?? "";
    }

    private void ValidateNamePart(string part, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(part))
            throw new ArgumentException($"{fieldName} es requerido.");

        string trimmed = part.Trim();

        if (!System.Text.RegularExpressions.Regex.IsMatch(trimmed, @"^[a-zA-ZáéíóúÁÉÍÓÚñÑ\s]+$"))
            throw new ArgumentException($"{fieldName} solo debe contener letras, sin números ni símbolos.");

        if (trimmed.Length < 2)
            throw new ArgumentException($"{fieldName} debe tener al menos 2 letras.");
    }

    private bool IsValidEmail(string email)
    {
        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            return addr.Address.Equals(email, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }
}
