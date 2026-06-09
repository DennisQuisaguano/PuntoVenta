using Application.Common;
using Application.Interfaces.Repositories;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _context;

    public CustomerRepository(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Customer?> GetByIdAsync(int id)
    {
        return await _context.Customers.FindAsync(id);
    }

    public async Task<Customer?> GetByIdCardAsync(string IDCard)
    {
        return await _context.Customers
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.IDCard == IDCard);
    }

    public async Task<Customer?> GetByIdCardIncludingInactiveAsync(string IDCard)
    {
        return await _context.Customers
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.IDCard == IDCard);
    }

    public async Task<PagedResponse<Customer>> GetPagedAsync(
        int pageNumber, 
        int pageSize, 
        string? term = null,
        string searchBy = "card",
        string status = "active")
    {
        IQueryable<Customer> query = _context.Customers.AsNoTracking();

        // Aplicar filtro de estado
        if (status == "inactive")
        {
            query = query.IgnoreQueryFilters().Where(c => !c.IsActive);
        }
        else if (status == "all")
        {
            query = query.IgnoreQueryFilters();
        }
        // "active" ya está cubierto por el Global Query Filter por defecto

        if (!string.IsNullOrWhiteSpace(term))
        {
            term = term.Trim().ToLower();

            query = searchBy.Trim().ToLower() switch
            {
                "id" => query.Where(c => c.Id.ToString().StartsWith(term)),
                "card" => query.Where(c => c.IDCard.StartsWith(term)),
                "name" => query.Where(c => c.LastName.ToLower().StartsWith(term) || c.Name.ToLower().StartsWith(term)),
                _ => query.Where(c => c.IDCard.StartsWith(term))
            };
        }

        var totalCount = await query.CountAsync();
        var items = await query
            .OrderBy(c => c.LastName)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedResponse<Customer>(items, totalCount, pageNumber, pageSize);
    }


    public async Task<IEnumerable<Customer>> SearchAsync(string term, string searchBy)
    {
        if (string.IsNullOrWhiteSpace(term))
            return Enumerable.Empty<Customer>();

        term = term.Trim().ToLower();
        searchBy = searchBy.Trim().ToLower();

        if (searchBy == "id")
        {
            if (int.TryParse(term, out int searchId))
            {
                return await _context.Customers
                 .AsNoTracking()
                 .Where(c => c.Id == searchId)
                 .Take(1) 
                 .ToListAsync();
            }
            return Enumerable.Empty<Customer>();
        }

        if (searchBy == "card")
        {
            return await _context.Customers
             .AsNoTracking()
             .Where(c => c.IDCard.StartsWith(term))
             .Take(30) 
             .ToListAsync();
        }

        return await _context.Customers
            .AsNoTracking()
            .Where(c =>
                c.Name.ToLower().StartsWith(term) ||
                c.LastName.ToLower().StartsWith(term))
            .OrderBy(c => c.LastName)
            .Take(30) 
            .ToListAsync();
    }

    public async Task AddAsync(Customer customer)
    {
        await _context.Customers.AddAsync(customer);
    }

    public async Task UpdateAsync(Customer customer)
    {
        _context.Customers.Update(customer);
    }

    public async Task DeleteAsync(int id)
    {
        var customer = await _context.Customers.FindAsync(id);
        if (customer != null)
        {
            _context.Customers.Remove(customer);
        }
    }

    public async Task<bool> HasRelatedRecordsAsync(int id)
    {
        return await _context.Sales.AnyAsync(s => s.CustomerId == id);
    }
}
