namespace Application.Interfaces;

public interface IDbProviderService
{
    string ProviderName { get; }
    Task<int> GetNextInvoiceSequenceAsync();
}
