using Domain.Common;
using FluentValidation;

namespace Application.Features.Users;

public class RegisterValidator : AbstractValidator<RegisterUserRequest>
{
    public RegisterValidator()
    {
        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio.")
            .MaximumLength(ValidationConstants.UserNameMax).WithMessage($"El usuario no puede exceder los {ValidationConstants.UserNameMax} caracteres.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(ValidationConstants.NameMax).WithMessage($"El nombre no puede exceder los {ValidationConstants.NameMax} caracteres.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("El apellido es obligatorio.")
            .MaximumLength(ValidationConstants.LastNameMax).WithMessage($"El apellido no puede exceder los {ValidationConstants.LastNameMax} caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
            .EmailAddress().WithMessage("El formato del correo electrónico no es válido.")
            .MaximumLength(ValidationConstants.EmailMax).WithMessage($"El correo no puede exceder los {ValidationConstants.EmailMax} caracteres.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("La contraseña es obligatoria.")
            .MaximumLength(ValidationConstants.PasswordMax).WithMessage($"La contraseña no puede exceder los {ValidationConstants.PasswordMax} caracteres.");
        
        RuleFor(x => x.Role)
            .NotEmpty().WithMessage("El rol es obligatorio.");
    }
}

public class UpdateUserValidator : AbstractValidator<UpdateUserRequest>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.UserName)
            .NotEmpty().WithMessage("El nombre de usuario es obligatorio.")
            .MaximumLength(ValidationConstants.UserNameMax).WithMessage($"El usuario no puede exceder los {ValidationConstants.UserNameMax} caracteres.");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("El nombre es obligatorio.")
            .MaximumLength(ValidationConstants.NameMax).WithMessage($"El nombre no puede exceder los {ValidationConstants.NameMax} caracteres.");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("El apellido es obligatorio.")
            .MaximumLength(ValidationConstants.LastNameMax).WithMessage($"El apellido no puede exceder los {ValidationConstants.LastNameMax} caracteres.");

        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("El correo electrónico es obligatorio.")
            .EmailAddress().WithMessage("El formato del correo electrónico no es válido.")
            .MaximumLength(ValidationConstants.EmailMax).WithMessage($"El correo no puede exceder los {ValidationConstants.EmailMax} caracteres.");
    }
}
