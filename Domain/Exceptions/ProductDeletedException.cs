using System;
using System.Collections.Generic;

namespace Domain.Exceptions;

public record DeletedProductInfo(int ProductId, string ProductName);

public class ProductDeletedException : Exception
{
    public List<DeletedProductInfo> DeletedProducts { get; }

    public ProductDeletedException(List<DeletedProductInfo> deletedProducts)
        : base(GenerateMessage(deletedProducts))
    {
        DeletedProducts = deletedProducts;
    }

    private static string GenerateMessage(List<DeletedProductInfo> deletedProducts)
    {
        if (deletedProducts == null || !deletedProducts.Any())
        {
            return "Se produjo un error inesperado al procesar los productos.";
        }

        if (deletedProducts.Count == 1)
        {
            var product = deletedProducts.First();
            return $"El producto '{product.ProductName}' (ID: {product.ProductId}) ya no se encuentra disponible.";
        }

        var productNames = string.Join(", ", deletedProducts.Select(p => p.ProductName));
        return $"Los siguientes productos ya no están disponibles: {productNames}.";
    }
}