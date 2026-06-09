using Microsoft.EntityFrameworkCore;
using Application.Interfaces.Repositories;
using Application.Common;
using Application.Features.Settings;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.Extensions.Options;

namespace Infrastructure.Repositories;

public class ProductRepository : IProductRepository
{
    private readonly AppDbContext _context;
    private readonly BusinessSettings _settings;

    public ProductRepository(AppDbContext context, IOptions<BusinessSettings> settings)
    {
        _context = context;
        _settings = settings.Value;
    }

    public async Task<Product?> GetByIdAsync(int id) 
    { 
        // Usar IgnoreQueryFilters para permitir obtener incluso si está inactivo (para reactivación)
        return await _context.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<IEnumerable<Product>> GetByIdsAsync(IEnumerable<int> ids)
    {
        return await _context.Products
            .Where(p => ids.Contains(p.Id))
            .ToListAsync();
    }

    public async Task<Product?> GetByNameAsync(string name)
    {
        return await _context.Products
            .IgnoreQueryFilters() // Para detectar duplicados incluso en eliminados
            .FirstOrDefaultAsync(p => p.Name == name.Trim().ToUpper());
    }

    public async Task<IEnumerable<Product>> SearchAsync(string term, string searchBy)
    {
        term = term.Trim().ToLower();

        if (searchBy.ToLower() == "id")
        {
            if (int.TryParse(term, out int searchId))
            {
                return await _context.Products
                    .AsNoTracking()
                    .Where(p => p.Id == searchId)
                    .Take(1)
                    .ToListAsync();
            }
            return Enumerable.Empty<Product>();
        }

        return await _context.Products
            .AsNoTracking()
            .Where(p => p.Name.ToLower().StartsWith(term))
            .OrderByDescending(p => p.Stock > 0)
            .ThenBy(p => p.Name)
            .Take(_settings.DefaultPageSize)
            .ToListAsync();
    }

    public async Task<PagedResponse<Product>> GetPagedAsync(int pageNumber, int pageSize, string? term = null, string searchBy = "name", string status = "active")
    {
        IQueryable<Product> query = _context.Products.AsNoTracking();

        // Aplicar filtro de estado
        if (status == "inactive")
        {
            query = query.IgnoreQueryFilters().Where(p => !p.IsActive);
        }
        else if (status == "all")
        {
            query = query.IgnoreQueryFilters();
        }

        if (!string.IsNullOrWhiteSpace(term))
        {
            term = term.Trim();

            var criterion = searchBy.ToLower();

            if (criterion == "id")
            {
                query = query.Where(p => p.Id.ToString().StartsWith(term));
            }
            else 
            {
                query = query.Where(p => p.Name.StartsWith(term));
            }
        }

        var totalCount = await query.CountAsync();

        var items = await query
        .OrderByDescending(p => p.IsActive) // Primero activos si es "all"
        .ThenByDescending(p => p.Stock > 0)
        .ThenBy(p => p.Name)
        .Skip((pageNumber - 1) * pageSize)
        .Take(pageSize)
        .ToListAsync();

        return new PagedResponse<Product>(items, totalCount, pageNumber, pageSize);
    }

    public async Task AddAsync(Product product)
    {
        await _context.Products.AddAsync(product);
    }

    public async Task UpdateAsync(Product product)
    {
        _context.Products.Update(product);
    }

    public async Task DeleteAsync(int id)
    {
        var product = await _context.Products.FindAsync(id);
        if (product != null)
        {
            var movements = await _context.InventoryMovements.Where(im => im.ProductId == id).ToListAsync();
            if (movements.Any())
            {
                _context.InventoryMovements.RemoveRange(movements);
            }
            
            // Hard delete
            _context.Products.Remove(product);
        }
    }

    public async Task<bool> HasRelatedRecordsAsync(int id)
    {
        return await _context.SaleDetails.AnyAsync(sd => sd.ProductId == id);
    }

    public async Task ReactivateAsync(int id)
    {
        var product = await _context.Products.IgnoreQueryFilters().FirstOrDefaultAsync(p => p.Id == id);
        if (product != null)
        {
            product.Activate();
        }
    }

    public async Task UpdateRangeAsync(IEnumerable<Product> products)
    {
        _context.Products.UpdateRange(products);
    }
}
