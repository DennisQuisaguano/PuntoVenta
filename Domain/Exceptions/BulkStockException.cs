using System;
using System.Collections.Generic;

namespace Domain.Exceptions;

public record StockValidationError(int ProductId, string ProductName, int RequestedQuantity, int AvailableStock);

public class BulkStockException : Exception
{
    public List<StockValidationError> Errors { get; }

    public BulkStockException(List<StockValidationError> errors)
        : base(GenerateMessage(errors))
    {
        Errors = errors;
    }

    private static string GenerateMessage(List<StockValidationError> errors)
    {
        if (errors == null || !errors.Any())
        {
            return "Se produjo un error de stock inesperado";
        }

        if (errors.Count == 1)
        {
            var error = errors.First();
            return $"El producto '{error.ProductName}' no tiene stock suficiente. " + 
                   $"(Solicitado: {error.RequestedQuantity}, Disponible: {error.AvailableStock})";
        }

        return $"Se encontraron {errors.Count} productos con problemas de stock.";
    }
}