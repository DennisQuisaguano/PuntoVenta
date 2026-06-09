using Application.Features.Customers;
using Application.Common;
using Blazor.Models;
using System.Net.Http.Json;

namespace Blazor.Services;

public class CustomerApiService
{
    private readonly HttpClient _http;
    public CustomerApiService(HttpClient http) => _http = http;
    public async Task<PagedResponse<CustomerResponseDto>?> SearchCustomersAsync(
        string? term,
        string criterion,
        int page,
        int size = 10,
        string status = "active")
    {
        var escapedTerm = System.Uri.EscapeDataString(term ?? string.Empty);
        var url = $"api/Customer/search?term={escapedTerm}&criterion={criterion}&page={page}&size={size}&status={status}";
        return await _http.GetFromJsonAsync<PagedResponse<CustomerResponseDto>>(url);
    }

    public async Task<CustomerResponseDto?> GetCustomerByIdAsync(int id)
    {
        return await _http.GetFromJsonAsync<CustomerResponseDto>($"api/Customer/{id}");
    }

    public async Task<HttpResponseMessage> CreateCustomerAsync(CreateCustomerRequest request)
    {
        return await _http.PostAsJsonAsync("api/Customer", request);
    }

    public async Task<HttpResponseMessage> UpdateCustomerAsync(int id, UpdateCustomerRequest request)
    {
        return await _http.PutAsJsonAsync($"api/Customer/{id}", request);
    }

    public async Task<HttpResponseMessage> ReactivateCustomerAsync(int id, UpdateCustomerRequest request)
    {
        return await _http.PutAsJsonAsync($"api/Customer/reactivate/{id}", request);
    }

    public async Task<HttpResponseMessage> DeleteCustomerAsync(int id)
    {
        return await _http.DeleteAsync($"api/Customer/{id}");
    }
}
