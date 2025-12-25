using System.Net.Http.Json;
using Blazored.SessionStorage;

namespace LaborControl.Shared.Services;

public class AuthService
{
    private readonly HttpClient _httpClient;
    private readonly ISessionStorageService _sessionStorage;

    public AuthService(HttpClient httpClient, ISessionStorageService sessionStorage)
    {
        _httpClient = httpClient;
        _sessionStorage = sessionStorage;
    }

    public async Task<(bool Success, bool RequiresPasswordChange, string? UserId)> LoginAsync(string email, string password)
    {
        var request = new LoginRequest { Email = email, Password = password };
        var response = await _httpClient.PostAsJsonAsync("api/Auth/login", request);

        if (response.IsSuccessStatusCode)
        {
            var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseDto>();
            if (loginResponse != null)
            {
                await _sessionStorage.SetItemAsync("authToken", loginResponse.Token);
                await _sessionStorage.SetItemAsync("userEmail", loginResponse.User.Email ?? "");
                await _sessionStorage.SetItemAsync("userRole", loginResponse.User.Role);
                await _sessionStorage.SetItemAsync("customerId", loginResponse.User.CustomerId.ToString());
                await _sessionStorage.SetItemAsync("userId", loginResponse.User.Id.ToString());

                return (true, loginResponse.RequiresPasswordChange, loginResponse.User.Id.ToString());
            }
        }
        return (false, false, null);
    }

    private class LoginResponseDto
    {
        public string Token { get; set; } = "";
        public bool RequiresPasswordChange { get; set; }
        public UserDto User { get; set; } = new();
    }

    private class UserDto
    {
        public Guid Id { get; set; }
        public string? Email { get; set; }
        public string? Username { get; set; }
        public string Role { get; set; } = "";
        public Guid CustomerId { get; set; }
    }

    public async Task<(bool Success, string? ErrorMessage)> RegisterProfessional(
        string email,
        string password,
        string companyName,
        string siret,
        string address,
        string postalCode,
        string city,
        string? website,
        string firstName,
        string lastName,
        string phone,
        string jobTitle,
        string service)
    {
        var request = new
        {
            Email = email,
            Password = password,
            CompanyName = companyName,
            Siret = siret,
            Address = address,
            PostalCode = postalCode,
            City = city,
            Website = website,
            FirstName = firstName,
            LastName = lastName,
            Phone = phone,
            JobTitle = jobTitle,
            Service = service
        };

        var response = await _httpClient.PostAsJsonAsync("api/Auth/register-professional", request);

        if (response.IsSuccessStatusCode)
        {
            // Authentifier automatiquement après inscription
            var (loginSuccess, _, _) = await LoginAsync(email, password);
            return (loginSuccess, null);
        }

        // Lire le message d'erreur de l'API
        try
        {
            var errorResponse = await response.Content.ReadFromJsonAsync<RegisterErrorResponse>();
            return (false, errorResponse?.Message ?? "Échec de l'inscription");
        }
        catch
        {
            return (false, "Échec de l'inscription. Vérifiez vos informations.");
        }
    }

    private class RegisterErrorResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
    }

    public async Task LogoutAsync()
    {
        await _sessionStorage.RemoveItemAsync("authToken");
        await _sessionStorage.RemoveItemAsync("userEmail");
        await _sessionStorage.RemoveItemAsync("userRole");
    }

    public async Task<bool> IsAuthenticatedAsync()
    {
        var token = await _sessionStorage.GetItemAsync<string>("authToken");
        return !string.IsNullOrEmpty(token);
    }

    public async Task<string?> GetTokenAsync()
    {
        return await _sessionStorage.GetItemAsync<string>("authToken");
    }

    public async Task<string?> GetUserEmailAsync()
    {
        return await _sessionStorage.GetItemAsync<string>("userEmail");
    }

    public async Task<string?> GetUserRoleAsync()
    {
        return await _sessionStorage.GetItemAsync<string>("userRole");
    }

    public async Task<string?> GetUserTypeAsync()
    {
        var role = await GetUserRoleAsync();

        // STAFF = rôles administratifs
        if (role == "ADMIN" || role == "SUPERVISOR")
            return "STAFF";

        // CLIENT = rôles opérationnels
        if (role == "CUSTOMER" || role == "TECHNICIAN")
            return "CLIENT";

        return null;
    }
}

public class LoginRequest
{
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
}
