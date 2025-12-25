using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace LaborControl.API.Services.AI
{
    /// <summary>
    /// Gemini OCR Service for qualification document pre-validation
    /// Uses Google Gemini Vision API for document analysis
    /// </summary>
    public class GeminiOCRService : IGeminiOCRService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<GeminiOCRService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;
        private readonly string _model;
        private const string GEMINI_API_URL = "https://generativelanguage.googleapis.com/v1beta/models/{0}:generateContent";

        public GeminiOCRService(
            HttpClient httpClient,
            ILogger<GeminiOCRService> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
            _apiKey = configuration["Gemini:ApiKey"] ?? throw new InvalidOperationException("Gemini API key not configured");
            _model = configuration["Gemini:Model"] ?? "gemini-1.5-flash";
        }

        public async Task<QualificationPreValidationResult> PreValidateQualificationAsync(QualificationPreValidationRequest request)
        {
            try
            {
                var prompt = $@"Analysez ce document de qualification {request.QualificationType}.
{(request.ExpectedStandard != null ? $"Norme attendue: {request.ExpectedStandard}" : "")}

Extrayez les informations suivantes au format JSON:
{{
    ""qualification_number"": ""numéro de qualification"",
    ""holder_name"": ""nom du titulaire"",
    ""qualification_type"": ""type (WELDER, NDT_VT, NDT_PT, etc.)"",
    ""welding_process"": ""procédé si applicable (TIG, MIG, etc.)"",
    ""certification_level"": ""niveau de certification (1, 2, 3) si applicable"",
    ""qualified_materials"": ""matériaux qualifiés"",
    ""thickness_range"": ""plage d'épaisseur"",
    ""diameter_range"": ""plage de diamètre"",
    ""qualified_positions"": ""positions qualifiées (1G, 2G, etc.)"",
    ""qualification_standard"": ""norme de qualification"",
    ""certifying_body"": ""organisme certificateur"",
    ""issue_date"": ""date d'émission (format YYYY-MM-DD)"",
    ""expiration_date"": ""date d'expiration (format YYYY-MM-DD)"",
    ""confidence_score"": ""score de confiance de 0 à 1"",
    ""warnings"": [""liste des alertes ou anomalies détectées""],
    ""validation_issues"": [""liste des problèmes de validation""]
}}

Si une information n'est pas lisible ou absente, indiquez null.
Vérifiez la cohérence des dates et la validité du document.";

                var response = await SendGeminiVisionRequestAsync(request.DocumentBase64, request.DocumentMimeType, prompt);

                if (!response.Success)
                {
                    return new QualificationPreValidationResult
                    {
                        Success = false,
                        Error = response.Error
                    };
                }

                // Parse the JSON response
                var extractedData = ParseQualificationData(response.Content);

                return new QualificationPreValidationResult
                {
                    Success = true,
                    ConfidenceScore = extractedData.ConfidenceScore,
                    ExtractedData = extractedData.Data,
                    Warnings = extractedData.Warnings,
                    ValidationIssues = extractedData.ValidationIssues,
                    AIModelVersion = _model
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pre-validating qualification document");
                return new QualificationPreValidationResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public async Task<DocumentExtractionResult> ExtractDocumentMetadataAsync(DocumentExtractionRequest request)
        {
            try
            {
                var prompt = $@"Analysez ce document technique de type: {request.DocumentType}

Extrayez les informations suivantes au format JSON:
{{
    ""document_reference"": ""référence du document"",
    ""document_title"": ""titre du document"",
    ""version"": ""version"",
    ""revision_index"": ""indice de révision"",
    ""issue_date"": ""date d'émission (YYYY-MM-DD)"",
    ""issuer"": ""émetteur"",
    ""applicable_standards"": [""liste des normes applicables""],
    ""keywords"": [""mots-clés extraits""],
    ""summary"": ""résumé du contenu (max 500 caractères)"",
    {(request.ExtractText ? @"""full_text"": ""texte complet extrait""," : "")}
    ""metadata"": {{
        ""equipment_reference"": ""référence équipement si applicable"",
        ""weld_class"": ""classe de soudure si applicable"",
        ""material_type"": ""type de matériau si applicable""
    }}
}}";

                var response = await SendGeminiVisionRequestAsync(request.DocumentBase64, request.DocumentMimeType, prompt);

                if (!response.Success)
                {
                    return new DocumentExtractionResult
                    {
                        Success = false,
                        Error = response.Error
                    };
                }

                // Parse the JSON response
                var extracted = ParseDocumentMetadata(response.Content);

                return new DocumentExtractionResult
                {
                    Success = true,
                    ExtractedText = extracted.FullText,
                    ExtractedMetadata = extracted.Metadata,
                    DocumentSummary = extracted.Summary,
                    DetectedKeywords = extracted.Keywords,
                    AIModelVersion = _model
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting document metadata");
                return new DocumentExtractionResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        private async Task<GeminiResponse> SendGeminiVisionRequestAsync(string base64Content, string mimeType, string prompt)
        {
            try
            {
                var url = string.Format(GEMINI_API_URL, _model) + $"?key={_apiKey}";

                var requestBody = new
                {
                    contents = new[]
                    {
                        new
                        {
                            parts = new object[]
                            {
                                new
                                {
                                    inline_data = new
                                    {
                                        mime_type = mimeType,
                                        data = base64Content
                                    }
                                },
                                new
                                {
                                    text = prompt
                                }
                            }
                        }
                    },
                    generationConfig = new
                    {
                        temperature = 0.1,
                        maxOutputTokens = 8192
                    }
                };

                var request = new HttpRequestMessage(HttpMethod.Post, url);
                request.Content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Gemini API error: {StatusCode} - {Response}", response.StatusCode, responseContent);
                    return new GeminiResponse
                    {
                        Success = false,
                        Error = $"API Error: {response.StatusCode}"
                    };
                }

                using var doc = JsonDocument.Parse(responseContent);
                var content = doc.RootElement
                    .GetProperty("candidates")[0]
                    .GetProperty("content")
                    .GetProperty("parts")[0]
                    .GetProperty("text")
                    .GetString();

                return new GeminiResponse
                {
                    Success = true,
                    Content = content
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Gemini API");
                return new GeminiResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        private (QualificationExtractedData? Data, decimal ConfidenceScore, List<string> Warnings, List<string> ValidationIssues) ParseQualificationData(string? content)
        {
            if (string.IsNullOrEmpty(content))
                return (null, 0, new List<string>(), new List<string>());

            try
            {
                // Find JSON in the response (might be wrapped in markdown code blocks)
                var jsonContent = ExtractJsonFromResponse(content);
                if (string.IsNullOrEmpty(jsonContent))
                    return (null, 0, new List<string> { "Could not parse response" }, new List<string>());

                using var doc = JsonDocument.Parse(jsonContent);
                var root = doc.RootElement;

                var data = new QualificationExtractedData
                {
                    QualificationNumber = GetStringOrNull(root, "qualification_number"),
                    HolderName = GetStringOrNull(root, "holder_name"),
                    QualificationType = GetStringOrNull(root, "qualification_type"),
                    WeldingProcess = GetStringOrNull(root, "welding_process"),
                    CertificationLevel = GetIntOrNull(root, "certification_level"),
                    QualifiedMaterials = GetStringOrNull(root, "qualified_materials"),
                    ThicknessRange = GetStringOrNull(root, "thickness_range"),
                    DiameterRange = GetStringOrNull(root, "diameter_range"),
                    QualifiedPositions = GetStringOrNull(root, "qualified_positions"),
                    QualificationStandard = GetStringOrNull(root, "qualification_standard"),
                    CertifyingBody = GetStringOrNull(root, "certifying_body"),
                    IssueDate = GetDateOrNull(root, "issue_date"),
                    ExpirationDate = GetDateOrNull(root, "expiration_date")
                };

                var confidenceScore = root.TryGetProperty("confidence_score", out var cs) ?
                    (decimal)cs.GetDouble() : 0.5m;

                var warnings = GetStringList(root, "warnings");
                var validationIssues = GetStringList(root, "validation_issues");

                return (data, confidenceScore, warnings, validationIssues);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing qualification data");
                return (null, 0, new List<string> { "Parse error: " + ex.Message }, new List<string>());
            }
        }

        private (string? FullText, Dictionary<string, string> Metadata, string? Summary, List<string> Keywords) ParseDocumentMetadata(string? content)
        {
            if (string.IsNullOrEmpty(content))
                return (null, new Dictionary<string, string>(), null, new List<string>());

            try
            {
                var jsonContent = ExtractJsonFromResponse(content);
                if (string.IsNullOrEmpty(jsonContent))
                    return (null, new Dictionary<string, string>(), null, new List<string>());

                using var doc = JsonDocument.Parse(jsonContent);
                var root = doc.RootElement;

                var metadata = new Dictionary<string, string>();

                if (root.TryGetProperty("document_reference", out var docRef))
                    metadata["document_reference"] = docRef.GetString() ?? "";
                if (root.TryGetProperty("document_title", out var docTitle))
                    metadata["document_title"] = docTitle.GetString() ?? "";
                if (root.TryGetProperty("version", out var version))
                    metadata["version"] = version.GetString() ?? "";
                if (root.TryGetProperty("issuer", out var issuer))
                    metadata["issuer"] = issuer.GetString() ?? "";
                if (root.TryGetProperty("issue_date", out var issueDate))
                    metadata["issue_date"] = issueDate.GetString() ?? "";

                var fullText = GetStringOrNull(root, "full_text");
                var summary = GetStringOrNull(root, "summary");
                var keywords = GetStringList(root, "keywords");

                return (fullText, metadata, summary, keywords);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error parsing document metadata");
                return (null, new Dictionary<string, string>(), null, new List<string>());
            }
        }

        private string? ExtractJsonFromResponse(string content)
        {
            // Try to extract JSON from markdown code blocks
            var jsonStart = content.IndexOf("```json");
            if (jsonStart >= 0)
            {
                var start = content.IndexOf('\n', jsonStart) + 1;
                var end = content.IndexOf("```", start);
                if (end > start)
                    return content[start..end].Trim();
            }

            // Try direct JSON
            var braceStart = content.IndexOf('{');
            var braceEnd = content.LastIndexOf('}');
            if (braceStart >= 0 && braceEnd > braceStart)
                return content[braceStart..(braceEnd + 1)];

            return null;
        }

        private string? GetStringOrNull(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.String)
                return prop.GetString();
            return null;
        }

        private int? GetIntOrNull(JsonElement element, string propertyName)
        {
            if (element.TryGetProperty(propertyName, out var prop))
            {
                if (prop.ValueKind == JsonValueKind.Number)
                    return prop.GetInt32();
                if (prop.ValueKind == JsonValueKind.String && int.TryParse(prop.GetString(), out var val))
                    return val;
            }
            return null;
        }

        private DateTime? GetDateOrNull(JsonElement element, string propertyName)
        {
            var str = GetStringOrNull(element, propertyName);
            if (!string.IsNullOrEmpty(str) && DateTime.TryParse(str, out var date))
                return date;
            return null;
        }

        private List<string> GetStringList(JsonElement element, string propertyName)
        {
            var list = new List<string>();
            if (element.TryGetProperty(propertyName, out var prop) && prop.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in prop.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.String)
                        list.Add(item.GetString() ?? "");
                }
            }
            return list;
        }

        private class GeminiResponse
        {
            public bool Success { get; set; }
            public string? Content { get; set; }
            public string? Error { get; set; }
        }
    }
}
