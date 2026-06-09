using Application.Interfaces;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using System.Data;

namespace Infrastructure.Services.Oracle;

public class OracleProviderService : IDbProviderService
{
    private readonly AppDbContext _context;

    public OracleProviderService(AppDbContext context)
    {
        _context = context;
    }

    public string ProviderName => "Oracle";

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
            
            // Sintaxis estándar de Oracle para secuencias
            command.CommandText = "SELECT InvoiceSequence.NEXTVAL FROM DUAL";

            var result = await command.ExecuteScalarAsync();
            return Convert.ToInt32(result);
        }
        finally
        {
            if (wasClosed) await connection.CloseAsync();
        }
    }
}
