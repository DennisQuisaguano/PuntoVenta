using Domain.Common;
using FluentValidation;

namespace Application.Features.Customers;

public class CreateCustomerValidator : AbstractValidator<CreateCustomerRequest>
{
    public CreateCustomerValidator()
    {
        RuleFor(x => x.IDCard)
            .NotEmpty().WithMessage("La cédula es obligatoria.")
            .Length(ValidationConstants.IDCardLength).WithMessage($"La cédula debe tener exactamente {ValidationConstants.IDCardLength} dígitos.")
            .Matches("^[0-9]*$").WithMessage("La cédula solo debe contener números.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(ValidationConstants.NameMax).WithMessage($"El nombre no puede exceder los {ValidationConstants.NameMax} caracteres.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("El apellido es obligatorio.")
            .MaximumLength(ValidationConstants.LastNameMax).WithMessage($"El apellido no puede exceder los {ValidationConstants.LastNameMax} caracteres.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("El teléfono es obligatorio.")
            .Length(ValidationConstants.PhoneLength).WithMessage($"El teléfono debe tener exactamente {ValidationConstants.PhoneLength} dígitos.")
            .Must(x => x != null && x.StartsWith("09")).WithMessage("El teléfono debe empezar con '09'.")
            .Matches("^[0-9]*$").WithMessage("El teléfono solo debe contener números.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
            .EmailAddress().WithMessage("El formato del correo electrónico no es válido.")
            .MaximumLength(ValidationConstants.EmailMax).WithMessage($"El correo no puede exceder los {ValidationConstants.EmailMax} caracteres.");

        RuleFor(x => x.Address)
            .MaximumLength(ValidationConstants.AddressMax).WithMessage($"La dirección no puede exceder los {ValidationConstants.AddressMax} caracteres.");
    }
}

public class UpdateCustomerValidator : AbstractValidator<UpdateCustomerRequest>
{
    public UpdateCustomerValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        
        RuleFor(x => x.IDCard)
            .NotEmpty().WithMessage("La cédula es obligatoria.")
            .Length(ValidationConstants.IDCardLength).WithMessage($"La cédula debe tener exactamente {ValidationConstants.IDCardLength} dígitos.")
            .Matches("^[0-9]*$").WithMessage("La cédula solo debe contener números.");

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(ValidationConstants.NameMax).WithMessage($"El nombre no puede exceder los {ValidationConstants.NameMax} caracteres.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("El apellido es obligatorio.")
            .MaximumLength(ValidationConstants.LastNameMax).WithMessage($"El apellido no puede exceder los {ValidationConstants.LastNameMax} caracteres.");

        RuleFor(x => x.Phone)
            .NotEmpty().WithMessage("El teléfono es obligatorio.")
            .Length(ValidationConstants.PhoneLength).WithMessage($"El teléfono debe tener exactamente {ValidationConstants.PhoneLength} dígitos.")
            .Must(x => x != null && x.StartsWith("09")).WithMessage("El teléfono debe empezar con '09'.")
            .Matches("^[0-9]*$").WithMessage("El teléfono solo debe contener números.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
            .EmailAddress().WithMessage("El formato del correo electrónico no es válido.")
            .MaximumLength(ValidationConstants.EmailMax).WithMessage($"El correo no puede exceder los {ValidationConstants.EmailMax} caracteres.");

        RuleFor(x => x.Address)
            .MaximumLength(ValidationConstants.AddressMax).WithMessage($"La dirección no puede exceder los {ValidationConstants.AddressMax} caracteres.");
    }
}
