using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace LaborControl.Dashboard.Services
{
    /// <summary>
    /// HTTP client for Labor Control API
    /// </summary>
    public class ApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ApiClient> _logger;
        private string? _accessToken;

        public ApiClient(HttpClient httpClient, IConfiguration configuration, ILogger<ApiClient> logger)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _logger = logger;

            var apiBaseUrl = configuration["Api:BaseUrl"] ?? "https://localhost:7001";
            _httpClient.BaseAddress = new Uri(apiBaseUrl);
            _httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        }

        public void SetAccessToken(string token)
        {
            _accessToken = token;
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public bool IsAuthenticated => !string.IsNullOrEmpty(_accessToken);

        #region Staff Authentication

        public async Task<StaffLoginResponse?> LoginAsync(string email, string password)
        {
            var request = new { email, password };
            var response = await PostAsync<object, StaffLoginResponse>("api/staff-auth/login", request);
            if (response?.Token != null)
            {
                SetAccessToken(response.Token);
            }
            return response;
        }

        public async Task<StaffUserDto?> GetCurrentUserAsync()
        {
            return await GetAsync<StaffUserDto>("api/staff-auth/me");
        }

        public void Logout()
        {
            _accessToken = null;
            _httpClient.DefaultRequestHeaders.Authorization = null;
        }

        #endregion

        #region Dashboard

        public async Task<WeldDashboardDto?> GetWeldDashboardAsync()
        {
            return await GetAsync<WeldDashboardDto>("api/welds/dashboard");
        }

        public async Task<List<NonConformityDto>?> GetOpenNonConformitiesAsync()
        {
            return await GetAsync<List<NonConformityDto>>("api/nonconformities?status=OPEN");
        }

        #endregion

        #region Welds

        public async Task<PaginatedResult<WeldDto>?> GetWeldsAsync(WeldFilterRequest? filter = null)
        {
            var query = filter?.ToQueryString() ?? "";
            return await GetAsync<PaginatedResult<WeldDto>>($"api/welds{query}");
        }

        public async Task<WeldDto?> GetWeldAsync(Guid id)
        {
            return await GetAsync<WeldDto>($"api/welds/{id}");
        }

        public async Task<WeldDto?> CreateWeldAsync(CreateWeldRequest request)
        {
            return await PostAsync<CreateWeldRequest, WeldDto>("api/welds", request);
        }

        public async Task<bool> UpdateWeldStatusAsync(Guid id, string status)
        {
            var response = await _httpClient.PatchAsync($"api/welds/{id}/status",
                new StringContent(JsonSerializer.Serialize(new { status }), Encoding.UTF8, "application/json"));
            return response.IsSuccessStatusCode;
        }

        #endregion

        #region Non-Conformities

        public async Task<PaginatedResult<NonConformityDto>?> GetNonConformitiesAsync(NCFilterRequest? filter = null)
        {
            var query = filter?.ToQueryString() ?? "";
            return await GetAsync<PaginatedResult<NonConformityDto>>($"api/nonconformities{query}");
        }

        public async Task<NonConformityDto?> GetNonConformityAsync(Guid id)
        {
            return await GetAsync<NonConformityDto>($"api/nonconformities/{id}");
        }

        public async Task<NonConformityDto?> CreateNonConformityAsync(CreateNCRequest request)
        {
            return await PostAsync<CreateNCRequest, NonConformityDto>("api/nonconformities", request);
        }

        #endregion

        #region AI Generation

        public async Task<AIGenerationResultDto?> GenerateDMOSAsync(DMOSGenerationRequest request)
        {
            return await PostAsync<DMOSGenerationRequest, AIGenerationResultDto>("api/ai/generate-dmos", request);
        }

        public async Task<AIGenerationResultDto?> GenerateNDTProgramAsync(NDTProgramGenerationRequest request)
        {
            return await PostAsync<NDTProgramGenerationRequest, AIGenerationResultDto>("api/ai/generate-ndt-program", request);
        }

        public async Task<AIRecommendationResultDto?> GetNCRecommendationAsync(NCRecommendationRequest request)
        {
            return await PostAsync<NCRecommendationRequest, AIRecommendationResultDto>("api/ai/nc-recommendation", request);
        }

        #endregion

        #region Welders & Qualifications

        public async Task<PaginatedResult<WelderDto>?> GetWeldersAsync(int page = 1, int pageSize = 20)
        {
            return await GetAsync<PaginatedResult<WelderDto>>($"api/welders?page={page}&pageSize={pageSize}");
        }

        public async Task<QualificationPreValidationResultDto?> PreValidateQualificationAsync(QualificationPreValidationRequest request)
        {
            return await PostAsync<QualificationPreValidationRequest, QualificationPreValidationResultDto>(
                "api/welderqualifications/pre-validate", request);
        }

        #endregion

        #region NDT Controls

        public async Task<PaginatedResult<NDTControlDto>?> GetNDTControlsAsync(NDTControlFilterRequest? filter = null)
        {
            var query = filter?.ToQueryString() ?? "";
            return await GetAsync<PaginatedResult<NDTControlDto>>($"api/ndtcontrols{query}");
        }

        #endregion

        #region HTTP Helpers

        public async Task<T?> GetAsync<T>(string endpoint)
        {
            try
            {
                var response = await _httpClient.GetAsync(endpoint);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("GET {Endpoint} failed: {StatusCode}", endpoint, response.StatusCode);
                    return default;
                }

                var content = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<T>(content, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling GET {Endpoint}", endpoint);
                return default;
            }
        }

        private async Task<TResponse?> PostAsync<TRequest, TResponse>(string endpoint, TRequest request)
        {
            try
            {
                var json = JsonSerializer.Serialize(request, JsonOptions);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(endpoint, content);
                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("POST {Endpoint} failed: {StatusCode}", endpoint, response.StatusCode);
                    return default;
                }

                var responseContent = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<TResponse>(responseContent, JsonOptions);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling POST {Endpoint}", endpoint);
                return default;
            }
        }

        private static JsonSerializerOptions JsonOptions => new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        #endregion
    }

    #region DTOs

    public class DashboardStats
    {
        public int TotalWelds { get; set; }
        public int CompletedWelds { get; set; }
        public int PendingNDT { get; set; }
        public int OpenNonConformities { get; set; }
        public int ActiveWelders { get; set; }
        public decimal WeeklyProgress { get; set; }
        public decimal AcceptanceRate { get; set; }
    }

    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    public class WeldDto
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string AssetName { get; set; } = string.Empty;
        public string WeldingProcess { get; set; } = string.Empty;
        public string? WelderName { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsCCPUValidated { get; set; }
        public int NDTControlsCount { get; set; }
        public int NonConformitiesCount { get; set; }
        public DateTime? ExecutedAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class WeldFilterRequest
    {
        public string? Status { get; set; }
        public string? Process { get; set; }
        public Guid? AssetId { get; set; }
        public string? Search { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public string ToQueryString()
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(Status)) parts.Add($"status={Status}");
            if (!string.IsNullOrEmpty(Process)) parts.Add($"process={Process}");
            if (AssetId.HasValue) parts.Add($"assetId={AssetId}");
            if (!string.IsNullOrEmpty(Search)) parts.Add($"search={Search}");
            parts.Add($"page={Page}");
            parts.Add($"pageSize={PageSize}");
            return "?" + string.Join("&", parts);
        }
    }

    public class CreateWeldRequest
    {
        public Guid AssetId { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string WeldingProcess { get; set; } = string.Empty;
        public Guid? WelderId { get; set; }
        public string? DMOSReference { get; set; }
    }

    public class NonConformityDto
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string NCType { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public Guid? WeldId { get; set; }
        public string? WeldReference { get; set; }
        public string Description { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public Guid? CreatedById { get; set; }
        public string? CreatedByName { get; set; }
        public DateTime? DetectionDate { get; set; }
        public DateTime? DueDate { get; set; }
        public bool RequiresRecontrol { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? CorrectiveAction { get; set; }
        public string? PreventiveAction { get; set; }
    }

    public class NCFilterRequest
    {
        public string? Status { get; set; }
        public string? Severity { get; set; }
        public string? NCType { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public string ToQueryString()
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(Status)) parts.Add($"status={Status}");
            if (!string.IsNullOrEmpty(Severity)) parts.Add($"severity={Severity}");
            if (!string.IsNullOrEmpty(NCType)) parts.Add($"ncType={NCType}");
            parts.Add($"page={Page}");
            parts.Add($"pageSize={PageSize}");
            return "?" + string.Join("&", parts);
        }
    }

    public class CreateNCRequest
    {
        public Guid? WeldId { get; set; }
        public string NCType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class WelderDto
    {
        public Guid Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string FullName => $"{Prenom} {Nom}";
        public string? CompanyName { get; set; }
        public int QualificationsCount { get; set; }
        public int ValidQualificationsCount { get; set; }
    }

    public class NDTControlDto
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string ControlType { get; set; } = string.Empty;
        public string? WeldReference { get; set; }
        public string ControllerName { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Result { get; set; }
        public DateTime? ExecutedAt { get; set; }
    }

    public class NDTControlFilterRequest
    {
        public string? Status { get; set; }
        public string? ControlType { get; set; }
        public Guid? WeldId { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;

        public string ToQueryString()
        {
            var parts = new List<string>();
            if (!string.IsNullOrEmpty(Status)) parts.Add($"status={Status}");
            if (!string.IsNullOrEmpty(ControlType)) parts.Add($"controlType={ControlType}");
            if (WeldId.HasValue) parts.Add($"weldId={WeldId}");
            parts.Add($"page={Page}");
            parts.Add($"pageSize={PageSize}");
            return "?" + string.Join("&", parts);
        }
    }

    // AI DTOs
    public class DMOSGenerationRequest
    {
        public string WeldingProcess { get; set; } = string.Empty;
        public string? BaseMaterials { get; set; }
        public string? ThicknessRange { get; set; }
        public string? Positions { get; set; }
        public string? AdditionalRequirements { get; set; }
    }

    public class NDTProgramGenerationRequest
    {
        public string? AssetName { get; set; }
        public string? WeldClass { get; set; }
        public string? CDCReference { get; set; }
        public string? AdditionalRequirements { get; set; }
    }

    public class NCRecommendationRequest
    {
        public string NCType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }

    public class AIGenerationResultDto
    {
        public bool Success { get; set; }
        public string? GeneratedContent { get; set; }
        public string? Error { get; set; }
        public int TokensUsed { get; set; }
    }

    public class AIRecommendationResultDto
    {
        public bool Success { get; set; }
        public string? CorrectiveActionRecommendation { get; set; }
        public string? PreventiveActionRecommendation { get; set; }
        public string? RootCauseAnalysis { get; set; }
        public string? Error { get; set; }
    }

    public class QualificationPreValidationRequest
    {
        public string DocumentBase64 { get; set; } = string.Empty;
        public string DocumentMimeType { get; set; } = string.Empty;
        public string QualificationType { get; set; } = string.Empty;
    }

    public class QualificationPreValidationResultDto
    {
        public bool Success { get; set; }
        public decimal ConfidenceScore { get; set; }
        public QualificationExtractedDataDto? ExtractedData { get; set; }
        public List<string> Warnings { get; set; } = new();
        public List<string> ValidationIssues { get; set; } = new();
        public string? Error { get; set; }
    }

    public class QualificationExtractedDataDto
    {
        public string? QualificationNumber { get; set; }
        public string? HolderName { get; set; }
        public string? QualificationType { get; set; }
        public string? WeldingProcess { get; set; }
        public DateTime? ExpirationDate { get; set; }
    }

    // Staff Authentication DTOs
    public class StaffLoginResponse
    {
        public string? Token { get; set; }
        public bool RequiresPasswordChange { get; set; }
        public StaffUserDto? User { get; set; }
    }

    public class StaffUserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Department { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public string FullName => $"{Prenom} {Nom}";
    }

    // Weld Dashboard DTO
    public class WeldDashboardDto
    {
        public int TotalWelds { get; set; }
        public int PlannedWelds { get; set; }
        public int InProgressWelds { get; set; }
        public int CompletedWelds { get; set; }
        public int PendingCCPUValidation { get; set; }
        public int PendingNDTControl { get; set; }
        public int WithNonConformities { get; set; }
        public int BlockedWelds { get; set; }
        public decimal ConformityRate { get; set; }
        public List<WeldStatusCount> StatusBreakdown { get; set; } = new();
    }

    public class WeldStatusCount
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    #endregion
}
