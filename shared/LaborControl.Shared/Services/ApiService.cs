using System.Net.Http.Json;
using System.Net.Http.Headers;
using LaborControl.Shared.Models;

namespace LaborControl.Shared.Services;

public class ApiService
{
    private readonly HttpClient _httpClient;
    private readonly AuthService _authService;

    public ApiService(HttpClient httpClient, AuthService authService)
    {
        _httpClient = httpClient;
        _authService = authService;
    }

    private async Task<HttpClient> GetAuthenticatedClientAsync()
    {
        var token = await _authService.GetTokenAsync();
        if (!string.IsNullOrEmpty(token))
        {
            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
        }
        return _httpClient;
    }

    // Generic GET
    public async Task<T?> GetAsync<T>(string endpoint)
    {
        var httpClient = await GetAuthenticatedClientAsync();
        return await httpClient.GetFromJsonAsync<T>(endpoint);
    }

    // Generic POST
    public async Task<bool> PostAsync<T>(string endpoint, T data)
    {
        var httpClient = await GetAuthenticatedClientAsync();
        var response = await httpClient.PostAsJsonAsync(endpoint, data);
        return response.IsSuccessStatusCode;
    }

    // Generic PUT
    public async Task<bool> PutAsync<T>(string endpoint, T data)
    {
        var httpClient = await GetAuthenticatedClientAsync();
        var response = await httpClient.PutAsJsonAsync(endpoint, data);
        return response.IsSuccessStatusCode;
    }

    // Generic DELETE
    public async Task<bool> DeleteAsync(string endpoint)
    {
        var httpClient = await GetAuthenticatedClientAsync();
        var response = await httpClient.DeleteAsync(endpoint);
        return response.IsSuccessStatusCode;
    }

    // Client-specific methods
    public async Task<List<Client>?> GetClientsAsync()
    {
        return await GetAsync<List<Client>>("api/customers");
    }

    public async Task<Client?> GetClientAsync(Guid clientId)
    {
        return await GetAsync<Client>($"api/customers/{clientId}");
    }

    public async Task<bool> UpdateClientAsync(Guid clientId, Client data)
    {
        return await PutAsync($"api/customers/{clientId}", data);
    }
}
