using Application.Interfaces.Repositories;
using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Repositories;

public class SaleRepository : ISaleRepository
{
    private readonly AppDbContext _context;
    private readonly IDbProviderService _dbProvider;

    public SaleRepository(AppDbContext context, IDbProviderService dbProvider)
    {
        _context = context;
        _dbProvider = dbProvider;
    }
    public async Task<string> GenerateInvoiceNumberAsync()
    {
        int sequential = await _dbProvider.GetNextInvoiceSequenceAsync();
        return $"FAC-{sequential:D7}";
    }

    public async Task AddAsync(Sale sale)
    {
        await _context.Sales.AddAsync(sale);
    }

    public async Task<IEnumerable<Sale>> SearchAsync(string term, string searchBy, string? sellerName = null, DateTime? startDate = null, DateTime? endDate = null, string? status = null)
    {
        term = term.Trim().ToLower();
        searchBy = searchBy.Trim().ToLower();   

        IQueryable<Sale> query = _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.Details)
                .ThenInclude(d => d.Product)
            .AsNoTracking();

        query = ApplyFilters(query, sellerName, startDate, endDate, status);

        if (searchBy == "id" && int.TryParse(term, out int searchId))
        {
            return await query
                .Where(s => s.Id == searchId)
                .ToListAsync();
        }

        if (searchBy == "recent")
        {
            return await query
                .OrderByDescending(s => s.IssueDate)
                .Take(50)
                .ToListAsync();
        }

        if (searchBy == "number")
        {
            return await query
                .Where(s => s.InvoiceNumber.ToLower().Contains(term))
                .OrderByDescending(s => s.IssueDate)
                .Take(30)
                .ToListAsync();
        }
        if (searchBy == "customer")
        {
            return await query
                .Where(s => s.Customer != null && (s.Customer.Name.ToLower().Contains(term) || s.Customer.LastName.ToLower().Contains(term)))
                .OrderByDescending(s => s.IssueDate)
                .Take(30)   
                .ToListAsync();
        }
        return Enumerable.Empty<Sale>();
    }

    public async Task<(IEnumerable<Sale> Items, int TotalCount)> GetPagedAsync(
        int page, 
        int pageSize, 
        string? term = null, 
        string? searchBy = null, 
        string? sellerName = null,
        DateTime? startDate = null,
        DateTime? endDate = null,
        string? status = null)
    {
        IQueryable<Sale> query = _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.Details)
                .ThenInclude(d => d.Product)
            .AsNoTracking();

        query = ApplyFilters(query, sellerName, startDate, endDate, status);

        if (!string.IsNullOrWhiteSpace(term))
        {
            term = term.Trim().ToLower();
            searchBy = searchBy?.Trim().ToLower() ?? "number";

            if (searchBy == "id" && int.TryParse(term, out int searchId))
            {
                query = query.Where(s => s.Id == searchId);
            }
            else if (searchBy == "number")
            {
                query = query.Where(s => s.InvoiceNumber.ToLower().Contains(term));
            }
            else if (searchBy == "customer")
            {
                query = query.Where(s => s.Customer != null && 
                    (s.Customer.Name.ToLower().Contains(term) || s.Customer.LastName.ToLower().Contains(term)));
            }
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderByDescending(s => s.IssueDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return (items, totalCount);
    }

    private IQueryable<Sale> ApplyFilters(IQueryable<Sale> query, string? sellerName, DateTime? startDate, DateTime? endDate, string? status = null)
    {
        // Filtro de vendedor
        if (!string.IsNullOrEmpty(sellerName))
        {
            query = query.Where(s => s.CreatedBy == sellerName);
        }

        // Filtro de Estado
        if (!string.IsNullOrEmpty(status) && status != "all")
        {
            if (Enum.TryParse<Domain.Enums.SaleStatus>(status, true, out var saleStatus))
            {
                query = query.Where(s => s.Status == saleStatus);
            }
        }

        // Filtro de Fecha Inicio (00:00:00)
        if (startDate.HasValue)
        {
            query = query.Where(s => s.IssueDate >= startDate.Value.Date);
        }

        // Filtro de Fecha Fin (23:59:59)
        if (endDate.HasValue)
        {
            var endOfDay = endDate.Value.Date.AddDays(1).AddTicks(-1);
            query = query.Where(s => s.IssueDate <= endOfDay);
        }

        return query;
    }

    public async Task<Sale?> GetByIdWithDetailsAsync(int id)
    {
        return await _context.Sales
            .Include(s => s.Customer)
            .Include(s => s.Details)
                .ThenInclude(d => d.Product)
            .FirstOrDefaultAsync(s => s.Id == id);
    }

    public async Task<IEnumerable<Sale>> GetMonthlySalesAsync(DateTime firstDayOfMonth)
    {
        return await _context.Sales
            .Where(s => s.IssueDate >= firstDayOfMonth)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<int> SaveSaleAsync(Sale sale)
    {
        await _context.Sales.AddAsync(sale);
        return sale.Id;
    }
}
