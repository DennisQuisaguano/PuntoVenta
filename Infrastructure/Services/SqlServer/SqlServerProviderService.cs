using Application.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace Infrastructure.Services.SqlServer;

public class SqlServerProviderService : IDbProviderService
{
    private readonly AppDbContext _context;

    public SqlServerProviderService(AppDbContext context)
    {
        _context = context;
    }

    public string ProviderName => "SqlServer";

    public async Task<int> GetNextInvoiceSequenceAsync()
    {
        var connection = _context.Database.GetDbConnection();
        bool wasClosed = connection.State == ConnectionState.Closed;

        if (wasClosed) await connection.OpenAsync();

        try
        {
            using var command = connection.CreateCommand();
            if (_context.Database.CurrentTransaction != null)
            {
                command.Transaction = _context.Database.CurrentTransaction.GetDbTransaction();
            }
            command.CommandText = "SELECT NEXT VALUE FOR InvoiceSequence";

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        finally
        {
            if (wasClosed) await connection.CloseAsync();
        }
    }
}
