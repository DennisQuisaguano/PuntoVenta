using Domain.Common;
using FluentValidation;

namespace Application.Features.Products;

public class CreateProductValidator : AbstractValidator<CreateProductRequest>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del producto es obligatorio.")
            .MaximumLength(ValidationConstants.ProductNameMax).WithMessage($"El nombre no puede exceder los {ValidationConstants.ProductNameMax} caracteres.");

        RuleFor(x => x.UnitPrice)
            .GreaterThan(0).WithMessage("El precio unitario debe ser mayor a cero.");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("El stock inicial no puede ser negativo.");
    }
}

public class UpdateProductValidator : AbstractValidator<UpdateProductRequest>
{
    public UpdateProductValidator()
    {
        RuleFor(x => x.Id).NotEmpty();

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("El nombre del producto es obligatorio.")
            .MaximumLength(ValidationConstants.ProductNameMax).WithMessage($"El nombre no puede exceder los {ValidationConstants.ProductNameMax} caracteres.");

        RuleFor(x => x.UnitPrice)
            .GreaterThan(0).WithMessage("El precio unitario debe ser mayor a cero.");

        RuleFor(x => x.Stock)
            .GreaterThanOrEqualTo(0).WithMessage("El stock no puede ser negativo.");
    }
}
