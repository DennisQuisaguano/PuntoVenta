using Application.Interfaces;
using Application.Interfaces.Repositories;
using Application.Features.Users;
using Domain.Entities;
using Domain.Enums;
using Domain.Exceptions;
using Microsoft.Extensions.Configuration;

namespace Application.Features.Sales;

public class SaleCommandService : ISaleCommandService
{
    private readonly ISaleRepository _saleRepository;
    private readonly IProductRepository _productRepository;
    private readonly ICustomerRepository _customerRepository;
    private readonly IInventoryRepository _inventoryRepository;
    private readonly IUserService _userService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public SaleCommandService(
        ISaleRepository saleRepository,
        IProductRepository productRepository,
        ICustomerRepository customerRepository,
        IInventoryRepository inventoryRepository,
        IUserService userService,
        IUnitOfWork unitOfWork,
        IConfiguration configuration)
    {
        _saleRepository = saleRepository;
        _productRepository = productRepository;
        _customerRepository = customerRepository;
        _inventoryRepository = inventoryRepository;
        _userService = userService;
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    public async Task<Sale> CreateSaleAsync(CreateSaleRequest request)
    {
        if (!request.Details.Any())
            throw new InvalidOperationException("La venta debe contener al menos un producto.");

        var customer = await _customerRepository.GetByIdAsync(request.CustomerId)
            ?? throw new KeyNotFoundException("El cliente no existe.");

        var currentUser = _unitOfWork.GetCurrentUser();
        var seller = await _userService.GetUserEntityByUserNameAsync(currentUser ?? throw new UnauthorizedAccessException("Sesión de usuario no válida."))
            ?? throw new KeyNotFoundException("No se pudo identificar al vendedor.");

        var deletedProducts = new List<DeletedProductInfo>();
        var validatedProducts = new List<(Product product, int amount)>();

        foreach (var item in request.Details)
        {
            var product = await _productRepository.GetByIdAsync(item.ProductId);

            if (product == null)
            {
                deletedProducts.Add(new DeletedProductInfo(item.ProductId, "Producto no encontrado"));
                continue;
            }

            validatedProducts.Add((product, item.Amount));
        }

        if (deletedProducts.Any()) throw new ProductDeletedException(deletedProducts);

        decimal vatRate = _configuration.GetValue<decimal>("BusinessSettings:VatRate");

        Sale sale;
        if (request.DraftId.HasValue && request.DraftId.Value > 0)
        {
            sale = await _saleRepository.GetByIdWithDetailsAsync(request.DraftId.Value);
            if (sale == null || sale.Status != SaleStatus.Draft)
                throw new InvalidOperationException("El borrador no existe o ya no está en estado Borrador.");

            sale.UpdateCustomerSnapshot(customer);
            sale.UpdateSellerSnapshot(seller);
            sale.ClearDetails();
        }
        else
        {
            string invoiceNumber = await _saleRepository.GenerateInvoiceNumberAsync();
            sale = new Sale(invoiceNumber, customer, seller, vatRate);
            await _saleRepository.AddAsync(sale);
        }

        foreach (var (product, amount) in validatedProducts)
        {
            sale.AddDetail(product, amount);
        }

        await _unitOfWork.SaveChangesAsync();

        return sale;
    }

    public async Task ConfirmSaleAsync(int saleId)
    {
        var sale = await _saleRepository.GetByIdWithDetailsAsync(saleId)
            ?? throw new KeyNotFoundException("Venta no encontrada.");

        if (sale.Status != SaleStatus.Draft)
            throw new InvalidOperationException("Solo se pueden confirmar ventas en estado Borrador.");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            var stockErrors = new List<StockValidationError>();

            foreach (var detail in sale.Details)
            {
                var product = await _productRepository.GetByIdAsync(detail.ProductId)
                    ?? throw new KeyNotFoundException($"Producto '{detail.ProductId}' no encontrado durante la confirmación.");

                if (product.Stock < detail.Amount)
                {
                    stockErrors.Add(new StockValidationError(product.Id, product.Name, detail.Amount, product.Stock));
                }
                else
                {
                    int previousStock = product.Stock;
                    product.DecreaseStock(detail.Amount);

                    var movement = new InventoryMovement(
                        product.Id,
                        MovementType.Sale,
                        detail.Amount,
                        previousStock,
                        product.Stock,
                        $"Confirmación Factura: {sale.InvoiceNumber}"
                    );
                    
                    await _inventoryRepository.AddMovementAsync(movement);
                }
            }

            if (stockErrors.Any()) throw new BulkStockException(stockErrors);

            sale.Confirm();

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task CancelSaleAsync(int saleId)
    {
        var sale = await _saleRepository.GetByIdWithDetailsAsync(saleId)
            ?? throw new KeyNotFoundException("Venta no encontrada.");

        if (sale.Status == SaleStatus.Cancelled)
            throw new InvalidOperationException("La venta ya se encuentra anulada.");

        await _unitOfWork.BeginTransactionAsync();
        try
        {
            if (sale.Status == SaleStatus.Confirmed)
            {
                foreach (var detail in sale.Details)
                {
                    var product = await _productRepository.GetByIdAsync(detail.ProductId)
                        ?? throw new KeyNotFoundException("Producto no encontrado durante la anulación.");

                    int previousStock = product.Stock;
                    product.IncreaseStock(detail.Amount);

                    var movement = new InventoryMovement(
                        product.Id,
                        MovementType.Return,
                        detail.Amount,
                        previousStock,
                        product.Stock,
                        $"Anulación Factura: {sale.InvoiceNumber}"
                    );
                    
                    await _inventoryRepository.AddMovementAsync(movement);
                }
            }

            sale.Cancel();

            await _unitOfWork.SaveChangesAsync();
            await _unitOfWork.CommitTransactionAsync();
        }
        catch
        {
            await _unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }
}
