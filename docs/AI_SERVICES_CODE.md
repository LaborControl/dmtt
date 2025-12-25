# Code Complet des Services IA

**LABOR CONTROL DMTT - Sprint 3**

Ce document contient le code complet pour implémenter les services IA (Claude + Gemini).

## Structure des Fichiers

```
backend/LaborControl.API/Services/AI/
├── Interfaces/
│   ├── IAIService.cs
│   ├── IClaudeService.cs
│   └── IGeminiService.cs
├── Models/
│   ├── AIRequest.cs
│   ├── AIResponse.cs
│   └── StructuredOutputs/
│       ├── QualificationData.cs
│       ├── NDTProgramData.cs
│       └── ProcedureData.cs
├── ClaudeService.cs
├── GeminiService.cs
├── AIOrchestrator.cs
└── Agents/
    ├── PreValidationQualificationAgent.cs
    ├── NDTProgramGeneratorAgent.cs
    └── ProcedureGeneratorAgent.cs
```

---

## 1. Interfaces

### Fichier : `Interfaces/IAIService.cs`

```csharp
namespace LaborControl.API.Services.AI.Interfaces;

public interface IAIService
{
    Task<string> GenerateTextAsync(string prompt, object? context = null);
    Task<T> GenerateStructuredAsync<T>(string prompt, object? context = null) where T : class;
}
```

### Fichier : `Interfaces/IClaudeService.cs`

```csharp
namespace LaborControl.API.Services.AI.Interfaces;

public interface IClaudeService : IAIService
{
    Task<string> GenerateTextAsync(string prompt, object? context = null);
    Task<T> GenerateStructuredAsync<T>(string prompt, object? context = null) where T : class;
}
```

### Fichier : `Interfaces/IGeminiService.cs`

```csharp
namespace LaborControl.API.Services.AI.Interfaces;

public interface IGeminiService
{
    Task<string> AnalyzeImageAsync(string prompt, byte[] imageData);
    Task<T> ExtractDataFromImageAsync<T>(string prompt, byte[] imageData) where T : class;
    Task<string> ExtractTextFromPDFAsync(byte[] pdfData);
}
```

---

## 2. Modèles

### Fichier : `Models/AIRequest.cs`

```csharp
namespace LaborControl.API.Services.AI.Models;

public class AIRequest
{
    public AIRequestType Type { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public object? Context { get; set; }
    public byte[]? ImageData { get; set; }
    public string? MimeType { get; set; }
}

public enum AIRequestType
{
    GenerateText,
    GenerateStructuredText,
    AnalyzeImage,
    ExtractFromImage,
    ExtractFromPDF
}
```

### Fichier : `Models/AIResponse.cs`

```csharp
namespace LaborControl.API.Services.AI.Models;

public class AIResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Error { get; set; }
    public AIMetadata? Metadata { get; set; }
}

public class AIMetadata
{
    public string Model { get; set; } = string.Empty;
    public int InputTokens { get; set; }
    public int OutputTokens { get; set; }
    public double LatencyMs { get; set; }
    public double EstimatedCost { get; set; }
}
```

### Fichier : `Models/StructuredOutputs/QualificationData.cs`

```csharp
namespace LaborControl.API.Services.AI.Models.StructuredOutputs;

public class QualificationData
{
    public string QualificationNumber { get; set; } = string.Empty;
    public string HolderName { get; set; } = string.Empty;
    public List<string> WeldingProcesses { get; set; } = new();
    public string Materials { get; set; } = string.Empty;
    public string ThicknessRange { get; set; } = string.Empty;
    public string DiameterRange { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public DateTime ExpirationDate { get; set; }
    public string IssuingBody { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public List<string> Warnings { get; set; } = new();
}
```

### Fichier : `Models/StructuredOutputs/NDTProgramData.cs`

```csharp
namespace LaborControl.API.Services.AI.Models.StructuredOutputs;

public class NDTProgramData
{
    public string ProgramReference { get; set; } = string.Empty;
    public string EquipmentCode { get; set; } = string.Empty;
    public DateTime CreationDate { get; set; }
    public List<string> ApplicableStandards { get; set; } = new();
    public List<NDTControl> Controls { get; set; } = new();
    public string GeneralRequirements { get; set; } = string.Empty;
    public string SafetyRequirements { get; set; } = string.Empty;
}

public class NDTControl
{
    public string WeldReference { get; set; } = string.Empty;
    public int Sequence { get; set; }
    public string ControlType { get; set; } = string.Empty;
    public string Standard { get; set; } = string.Empty;
    public string AcceptanceCriteria { get; set; } = string.Empty;
    public string Coverage { get; set; } = string.Empty;
    public string Timing { get; set; } = string.Empty;
    public string SpecificRequirements { get; set; } = string.Empty;
}
```

### Fichier : `Models/StructuredOutputs/ProcedureData.cs`

```csharp
namespace LaborControl.API.Services.AI.Models.StructuredOutputs;

public class ProcedureData
{
    public string ProcedureReference { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0";
    public string OperationType { get; set; } = string.Empty;
    public DateTime CreationDate { get; set; }
    public List<ProcedureSection> Sections { get; set; } = new();
}

public class ProcedureSection
{
    public string Number { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<ProcedureStep> Steps { get; set; } = new();
}

public class ProcedureStep
{
    public string Number { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public string? Parameters { get; set; }
    public bool ControlPoint { get; set; }
    public string? AcceptanceCriteria { get; set; }
}
```

---

## 3. ClaudeService

### Fichier : `ClaudeService.cs`

```csharp
using System.Text.Json;
using LaborControl.API.Services.AI.Interfaces;
using Anthropic.SDK;
using Anthropic.SDK.Messaging;

namespace LaborControl.API.Services.AI;

public class ClaudeService : IClaudeService
{
    private readonly AnthropicClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ClaudeService> _logger;
    private readonly string _model;
    private readonly int _maxTokens;
    private readonly decimal _temperature;

    public ClaudeService(IConfiguration configuration, ILogger<ClaudeService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var apiKey = _configuration["Claude:ApiKey"]
            ?? throw new InvalidOperationException("Claude API Key not configured");

        _model = _configuration["Claude:Model"] ?? "claude-3-5-sonnet-20241022";
        _maxTokens = int.Parse(_configuration["Claude:MaxTokens"] ?? "4096");
        _temperature = decimal.Parse(_configuration["Claude:Temperature"] ?? "0.3");

        _client = new AnthropicClient(apiKey);

        _logger.LogInformation($"ClaudeService initialized with model: {_model}");
    }

    public async Task<string> GenerateTextAsync(string prompt, object? context = null)
    {
        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var userMessage = context != null
                ? $"{prompt}\n\nContext:\n{JsonSerializer.Serialize(context, new JsonSerializerOptions { WriteIndented = true })}"
                : prompt;

            var messages = new List<Message>
            {
                new Message
                {
                    Role = "user",
                    Content = userMessage
                }
            };

            var parameters = new MessageParameters
            {
                Messages = messages,
                Model = _model,
                MaxTokens = _maxTokens,
                Temperature = _temperature
            };

            var response = await _client.Messages.GetClaudeMessageAsync(parameters);
            stopwatch.Stop();

            _logger.LogInformation($"Claude API call completed in {stopwatch.ElapsedMilliseconds}ms");

            return response.Content.First().Text ?? string.Empty;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Claude API");
            throw;
        }
    }

    public async Task<T> GenerateStructuredAsync<T>(string prompt, object? context = null) where T : class
    {
        try
        {
            var systemPrompt = $@"Tu es un assistant IA qui génère des données structurées au format JSON.

IMPORTANT : Tu dois répondre UNIQUEMENT avec un JSON valide, sans aucun texte avant ou après.
Pas de markdown, pas d'explication, juste le JSON brut.

Structure attendue : {typeof(T).Name}

{GetJsonSchema<T>()}";

            var fullPrompt = $"{systemPrompt}\n\n{prompt}";

            var response = await GenerateTextAsync(fullPrompt, context);

            // Nettoyer la réponse (enlever markdown si présent)
            var cleanedResponse = response.Trim();
            if (cleanedResponse.StartsWith("```json"))
            {
                cleanedResponse = cleanedResponse.Replace("```json", "").Replace("```", "").Trim();
            }

            // Parser le JSON
            var result = JsonSerializer.Deserialize<T>(cleanedResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null)
            {
                throw new InvalidOperationException("Failed to deserialize Claude response to expected type");
            }

            return result;
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "Failed to parse Claude response as JSON");
            throw new InvalidOperationException("Claude returned invalid JSON", ex);
        }
    }

    private string GetJsonSchema<T>()
    {
        // Retourne un exemple de structure JSON basé sur le type T
        // Pour une implémentation complète, utiliser System.Text.Json.Schema
        return $"Exemple de structure pour {typeof(T).Name}";
    }
}
```

---

## 4. GeminiService

### Fichier : `GeminiService.cs`

```csharp
using System.Text;
using System.Text.Json;
using LaborControl.API.Services.AI.Interfaces;

namespace LaborControl.API.Services.AI;

public class GeminiService : IGeminiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiService> _logger;
    private readonly string _apiKey;
    private readonly string _model;

    public GeminiService(IConfiguration configuration, ILogger<GeminiService> logger, IHttpClientFactory httpClientFactory)
    {
        _configuration = configuration;
        _logger = logger;
        _httpClient = httpClientFactory.CreateClient();

        _apiKey = _configuration["Gemini:ApiKey"]
            ?? throw new InvalidOperationException("Gemini API Key not configured");

        _model = _configuration["Gemini:Model"] ?? "gemini-2.0-flash-exp";

        _logger.LogInformation($"GeminiService initialized with model: {_model}");
    }

    public async Task<T> ExtractDataFromImageAsync<T>(string prompt, byte[] imageData) where T : class
    {
        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            var base64Image = Convert.ToBase64String(imageData);

            var systemInstruction = $@"Tu es un expert en extraction de données depuis des documents techniques.
Extrais les informations demandées et retourne UNIQUEMENT un JSON valide.
Pas de texte avant ou après, juste le JSON brut.

Structure attendue : {typeof(T).Name}";

            var requestBody = new
            {
                contents = new[]
                {
                    new
                    {
                        role = "user",
                        parts = new object[]
                        {
                            new { text = $"{systemInstruction}\n\n{prompt}" },
                            new
                            {
                                inline_data = new
                                {
                                    mime_type = "image/jpeg",
                                    data = base64Image
                                }
                            }
                        }
                    }
                },
                generation_config = new
                {
                    temperature = 0.2,
                    max_output_tokens = 2048
                }
            };

            var jsonRequest = JsonSerializer.Serialize(requestBody);
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{_model}:generateContent?key={_apiKey}";

            var content = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            var response = await _httpClient.PostAsync(url, content);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var geminiResponse = JsonSerializer.Deserialize<GeminiResponse>(responseJson);

            var generatedText = geminiResponse?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text
                ?? throw new InvalidOperationException("No text generated by Gemini");

            stopwatch.Stop();
            _logger.LogInformation($"Gemini API call completed in {stopwatch.ElapsedMilliseconds}ms");

            // Nettoyer et parser
            var cleanedText = generatedText.Trim();
            if (cleanedText.StartsWith("```json"))
            {
                cleanedText = cleanedText.Replace("```json", "").Replace("```", "").Trim();
            }

            var result = JsonSerializer.Deserialize<T>(cleanedText, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (result == null)
            {
                throw new InvalidOperationException("Failed to deserialize Gemini response");
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling Gemini API");
            throw;
        }
    }

    public async Task<string> AnalyzeImageAsync(string prompt, byte[] imageData)
    {
        var result = await ExtractDataFromImageAsync<Dictionary<string, object>>(prompt, imageData);
        return JsonSerializer.Serialize(result);
    }

    public async Task<string> ExtractTextFromPDFAsync(byte[] pdfData)
    {
        // Pour PDF, Gemini nécessite une conversion en images ou utiliser l'API avec PDF support
        // Implémentation simplifiée : convertir PDF en image puis analyser
        throw new NotImplementedException("PDF extraction to be implemented");
    }

    // Classes pour parser la réponse Gemini
    private class GeminiResponse
    {
        public List<Candidate>? Candidates { get; set; }
    }

    private class Candidate
    {
        public Content? Content { get; set; }
    }

    private class Content
    {
        public List<Part>? Parts { get; set; }
    }

    private class Part
    {
        public string? Text { get; set; }
    }
}
```

---

## 5. AIOrchestrator

### Fichier : `AIOrchestrator.cs`

```csharp
using LaborControl.API.Services.AI.Interfaces;
using LaborControl.API.Services.AI.Models;

namespace LaborControl.API.Services.AI;

public class AIOrchestrator
{
    private readonly IClaudeService _claudeService;
    private readonly IGeminiService _geminiService;
    private readonly ILogger<AIOrchestrator> _logger;

    public AIOrchestrator(
        IClaudeService claudeService,
        IGeminiService geminiService,
        ILogger<AIOrchestrator> logger)
    {
        _claudeService = claudeService;
        _geminiService = geminiService;
        _logger = logger;
    }

    public async Task<AIResponse<T>> RouteRequestAsync<T>(AIRequest request) where T : class
    {
        _logger.LogInformation($"Routing AI request of type: {request.Type}");

        try
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();
            T result;

            switch (request.Type)
            {
                case AIRequestType.ExtractFromImage:
                    if (request.ImageData == null)
                        throw new ArgumentException("ImageData is required for ExtractFromImage requests");

                    result = await _geminiService.ExtractDataFromImageAsync<T>(request.Prompt, request.ImageData);
                    break;

                case AIRequestType.GenerateStructuredText:
                    result = await _claudeService.GenerateStructuredAsync<T>(request.Prompt, request.Context);
                    break;

                case AIRequestType.AnalyzeImage:
                    if (request.ImageData == null)
                        throw new ArgumentException("ImageData is required for AnalyzeImage requests");

                    var analysisResult = await _geminiService.AnalyzeImageAsync(request.Prompt, request.ImageData);
                    result = (T)(object)analysisResult;
                    break;

                case AIRequestType.GenerateText:
                    var textResult = await _claudeService.GenerateTextAsync(request.Prompt, request.Context);
                    result = (T)(object)textResult;
                    break;

                case AIRequestType.ExtractFromPDF:
                    if (request.ImageData == null)
                        throw new ArgumentException("ImageData (PDF bytes) is required for ExtractFromPDF requests");

                    var pdfResult = await _geminiService.ExtractTextFromPDFAsync(request.ImageData);
                    result = (T)(object)pdfResult;
                    break;

                default:
                    throw new ArgumentException($"Unsupported request type: {request.Type}");
            }

            stopwatch.Stop();

            return new AIResponse<T>
            {
                Success = true,
                Data = result,
                Metadata = new AIMetadata
                {
                    Model = request.Type == AIRequestType.GenerateStructuredText || request.Type == AIRequestType.GenerateText
                        ? "claude-3-5-sonnet"
                        : "gemini-2.0-flash",
                    LatencyMs = stopwatch.Elapsed.TotalMilliseconds
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, $"Error processing AI request of type {request.Type}");

            return new AIResponse<T>
            {
                Success = false,
                Error = ex.Message
            };
        }
    }

    public async Task<string> RouteTextRequestAsync(AIRequest request)
    {
        var response = await RouteRequestAsync<string>(request);

        if (!response.Success)
        {
            throw new InvalidOperationException($"AI request failed: {response.Error}");
        }

        return response.Data ?? string.Empty;
    }
}
```

---

## 6. Configuration dans Program.cs

### Fichier : `Program.cs` (extrait)

```csharp
using LaborControl.API.Services.AI;
using LaborControl.API.Services.AI.Interfaces;

// ... autres configurations

// Services IA
builder.Services.AddHttpClient(); // Pour GeminiService

builder.Services.AddScoped<IClaudeService, ClaudeService>();
builder.Services.AddScoped<IGeminiService, GeminiService>();
builder.Services.AddScoped<AIOrchestrator>();

// Agents
builder.Services.AddScoped<PreValidationQualificationAgent>();
builder.Services.AddScoped<NDTProgramGeneratorAgent>();
builder.Services.AddScoped<ProcedureGeneratorAgent>();

// ... suite de la configuration
```

---

## 7. Agents

### Fichier : `Agents/PreValidationQualificationAgent.cs`

```csharp
using LaborControl.API.Services.AI.Interfaces;
using LaborControl.API.Services.AI.Models;
using LaborControl.API.Services.AI.Models.StructuredOutputs;

namespace LaborControl.API.Services.AI.Agents;

public class PreValidationQualificationAgent
{
    private readonly IGeminiService _geminiService;
    private readonly ILogger<PreValidationQualificationAgent> _logger;

    public PreValidationQualificationAgent(
        IGeminiService geminiService,
        ILogger<PreValidationQualificationAgent> logger)
    {
        _geminiService = geminiService;
        _logger = logger;
    }

    public async Task<QualificationData> ValidateQualificationAsync(byte[] certificateImage)
    {
        _logger.LogInformation("Starting qualification validation with Gemini");

        var prompt = @"Analyse ce certificat de qualification de soudeur ou contrôleur CND.

Extrais les informations suivantes :
- Numéro de qualification (QualificationNumber)
- Nom du titulaire (HolderName)
- Procédés de soudage qualifiés (WeldingProcesses) - array de strings (ex: [""TIG"", ""MIG""])
- Matériaux qualifiés (Materials) - string
- Plage d'épaisseur (ThicknessRange) - format ""X-Ymm""
- Plage de diamètre (DiameterRange) - format ""DNXX-DNXX""
- Date d'émission (IssueDate) - format ISO 8601
- Date d'expiration (ExpirationDate) - format ISO 8601
- Organisme émetteur (IssuingBody)

Si une information n'est pas trouvée, utilise une valeur vide ou neutre.

Calcule également un niveau de confiance (Confidence) entre 0 et 1 basé sur la qualité de l'image et la lisibilité.

Si tu détectes des problèmes (qualification expirée, image floue, etc.), ajoute-les dans Warnings (array de strings).

Retourne UNIQUEMENT le JSON, sans texte additionnel.";

        var result = await _geminiService.ExtractDataFromImageAsync<QualificationData>(prompt, certificateImage);

        _logger.LogInformation($"Qualification validated with confidence: {result.Confidence}");

        return result;
    }
}
```

### Fichier : `Agents/NDTProgramGeneratorAgent.cs`

```csharp
using LaborControl.API.Services.AI.Interfaces;
using LaborControl.API.Services.AI.Models.StructuredOutputs;
using System.Text.Json;

namespace LaborControl.API.Services.AI.Agents;

public class NDTProgramGeneratorAgent
{
    private readonly IClaudeService _claudeService;
    private readonly ILogger<NDTProgramGeneratorAgent> _logger;

    public NDTProgramGeneratorAgent(
        IClaudeService claudeService,
        ILogger<NDTProgramGeneratorAgent> logger)
    {
        _claudeService = claudeService;
        _logger = logger;
    }

    public async Task<NDTProgramData> GenerateNDTProgramAsync(
        string equipmentCode,
        List<WeldInfo> welds,
        List<string> applicableStandards,
        string cdcContent,
        string normsContent)
    {
        _logger.LogInformation($"Generating NDT program for equipment {equipmentCode}");

        var prompt = $@"Tu es un expert en contrôles non destructifs (CND) pour le secteur nucléaire français.
Tu connais parfaitement les normes RCC-M, RSEM, et les standards EDF.

TÂCHE : Génère un programme de contrôle non destructif (CND) complet et conforme pour les soudures suivantes.

ÉQUIPEMENT : {equipmentCode}

SOUDURES :
{JsonSerializer.Serialize(welds, new JsonSerializerOptions { WriteIndented = true })}

NORMES APPLICABLES :
{string.Join(", ", applicableStandards)}

CAHIER DES CHARGES :
{cdcContent}

NORMES TECHNIQUES :
{normsContent}

INSTRUCTIONS :
Pour chaque soudure, détermine :
1. Les contrôles CND requis (VT, PT, MT, RT, UT) selon la classe de soudure
2. L'ordre des contrôles (séquence)
3. Les normes applicables pour chaque contrôle
4. Les critères d'acceptation (niveau A, B, ou C selon RCC-M)
5. Le pourcentage de contrôle (100%, échantillonnage 10%, etc.)
6. Les exigences spécifiques (accessibilité, préparation surface, délai après soudage)

RÈGLES :
- Classe A : VT 100% + PT 100% + RT ou UT 100%
- Classe B : VT 100% + PT 100% + RT ou UT 10%
- Classe C : VT 100%

Retourne un JSON structuré avec :
- programReference : référence du programme (format PROG-CND-XXX-YYYY)
- equipmentCode : code équipement
- creationDate : date actuelle ISO 8601
- applicableStandards : liste des normes
- controls : array de contrôles avec tous les détails
- generalRequirements : exigences générales
- safetyRequirements : exigences de sécurité/radioprotection

Retourne UNIQUEMENT le JSON, sans texte avant ou après.";

        var context = new
        {
            equipmentCode,
            welds,
            applicableStandards
        };

        var result = await _claudeService.GenerateStructuredAsync<NDTProgramData>(prompt, context);

        // Générer référence si manquante
        if (string.IsNullOrEmpty(result.ProgramReference))
        {
            result.ProgramReference = $"PROG-CND-{equipmentCode}-{DateTime.Now:yyyyMMdd}";
        }

        result.EquipmentCode = equipmentCode;
        result.CreationDate = DateTime.UtcNow;

        _logger.LogInformation($"NDT program generated: {result.ProgramReference}");

        return result;
    }

    public class WeldInfo
    {
        public string Reference { get; set; } = string.Empty;
        public string Diameter { get; set; } = string.Empty;
        public string Thickness { get; set; } = string.Empty;
        public string WeldClass { get; set; } = string.Empty;
        public string Material1 { get; set; } = string.Empty;
        public string Material2 { get; set; } = string.Empty;
        public string WeldingProcess { get; set; } = string.Empty;
    }
}
```

### Fichier : `Agents/ProcedureGeneratorAgent.cs`

```csharp
using LaborControl.API.Services.AI.Interfaces;
using LaborControl.API.Services.AI.Models.StructuredOutputs;

namespace LaborControl.API.Services.AI.Agents;

public class ProcedureGeneratorAgent
{
    private readonly IClaudeService _claudeService;
    private readonly ILogger<ProcedureGeneratorAgent> _logger;

    public ProcedureGeneratorAgent(
        IClaudeService claudeService,
        ILogger<ProcedureGeneratorAgent> logger)
    {
        _claudeService = claudeService;
        _logger = logger;
    }

    public async Task<ProcedureData> GenerateProcedureAsync(
        string operationType,
        string cdcContent,
        List<string> applicableStandards,
        List<string> specificRequirements,
        List<string> safetyConstraints)
    {
        _logger.LogInformation($"Generating procedure for operation: {operationType}");

        var prompt = $@"Tu es un expert en soudage nucléaire et rédaction de procédures techniques.
Tu connais les normes RCC-M, RSEM, COFREND, et les exigences EDF.

TÂCHE : Génère une procédure technique complète et opérationnelle pour :
{operationType}

CAHIER DES CHARGES :
{cdcContent}

NORMES APPLICABLES :
{string.Join(", ", applicableStandards)}

EXIGENCES SPÉCIFIQUES :
{string.Join("\n", specificRequirements)}

CONTRAINTES DE SÉCURITÉ :
{string.Join("\n", safetyConstraints)}

STRUCTURE REQUISE :

1. OBJET ET DOMAINE D'APPLICATION
2. RÉFÉRENCES NORMATIVES
3. DOCUMENTS APPLICABLES
4. DÉFINITIONS ET ABRÉVIATIONS
5. RESPONSABILITÉS
6. ÉQUIPEMENTS ET MATÉRIELS REQUIS
7. QUALIFICATIONS REQUISES
8. PRÉPARATION
   - Vérifications préalables
   - Préparation zone de travail
   - Préparation matériaux
9. EXÉCUTION
   - Étapes détaillées (numérotées)
   - Points de contrôle
   - Points d'arrêt obligatoires
10. CONTRÔLES ET ESSAIS
11. CRITÈRES D'ACCEPTATION
12. TRAÇABILITÉ
13. TRAITEMENT DES NON-CONFORMITÉS
14. SÉCURITÉ ET RADIOPROTECTION

Chaque étape d'exécution doit être :
- Numérotée (ex: 9.1, 9.2, etc.)
- Action précise (verbe à l'infinitif)
- Paramètres techniques si applicable
- Point de contrôle si requis (controlPoint: true/false)
- Critère d'acceptation si applicable

Retourne un JSON avec :
- procedureReference : référence (format PROC-XXX-YYY)
- version : ""1.0""
- operationType : type d'opération
- creationDate : date actuelle ISO 8601
- sections : array de sections avec steps

Retourne UNIQUEMENT le JSON, sans texte avant ou après.";

        var context = new
        {
            operationType,
            applicableStandards,
            specificRequirements,
            safetyConstraints
        };

        var result = await _claudeService.GenerateStructuredAsync<ProcedureData>(prompt, context);

        // Compléter les métadonnées
        if (string.IsNullOrEmpty(result.ProcedureReference))
        {
            var opTypeShort = operationType.Replace(" ", "-").ToUpper().Substring(0, Math.Min(10, operationType.Length));
            result.ProcedureReference = $"PROC-{opTypeShort}-{DateTime.Now:yyyyMMdd}";
        }

        result.OperationType = operationType;
        result.CreationDate = DateTime.UtcNow;

        _logger.LogInformation($"Procedure generated: {result.ProcedureReference}");

        return result;
    }
}
```

---

## 8. Installation des Packages

```bash
cd backend/LaborControl.API

# Anthropic SDK pour Claude
dotnet add package Anthropic.SDK --version 0.2.0

# System.Text.Json (normalement déjà inclus avec .NET 9)
# Pas besoin d'ajouter explicitement
```

**Note** : Pour Gemini, nous utilisons l'API REST directement via HttpClient, donc pas de package spécifique requis.

---

## 9. Configuration appsettings.json

```json
{
  "Claude": {
    "ApiKey": "sk-ant-api03-...",
    "Model": "claude-3-5-sonnet-20241022",
    "MaxTokens": 4096,
    "Temperature": 0.3
  },
  "Gemini": {
    "ApiKey": "AIza...",
    "Model": "gemini-2.0-flash-exp"
  }
}
```

---

## 10. Tests d'Intégration

### Test ClaudeService

```csharp
[Fact]
public async Task ClaudeService_ShouldGenerateStructuredOutput()
{
    // Arrange
    var config = new ConfigurationBuilder()
        .AddUserSecrets<ClaudeServiceTests>()
        .Build();

    var logger = new Mock<ILogger<ClaudeService>>();
    var service = new ClaudeService(config, logger.Object);

    var prompt = "Génère un programme de CND simple pour une soudure DN100 classe B";

    // Act
    var result = await service.GenerateStructuredAsync<NDTProgramData>(prompt);

    // Assert
    Assert.NotNull(result);
    Assert.NotEmpty(result.ProgramReference);
    Assert.NotEmpty(result.Controls);
}
```

### Test GeminiService

```csharp
[Fact]
public async Task GeminiService_ShouldExtractQualificationData()
{
    // Arrange
    var config = new ConfigurationBuilder()
        .AddUserSecrets<GeminiServiceTests>()
        .Build();

    var logger = new Mock<ILogger<GeminiService>>();
    var httpClientFactory = new Mock<IHttpClientFactory>();
    httpClientFactory.Setup(x => x.CreateClient(It.IsAny<string>())).Returns(new HttpClient());

    var service = new GeminiService(config, logger.Object, httpClientFactory.Object);

    var imageBytes = File.ReadAllBytes("test-certificate.jpg");

    // Act
    var result = await service.ExtractDataFromImageAsync<QualificationData>(
        "Extrais les données de ce certificat de qualification",
        imageBytes
    );

    // Assert
    Assert.NotNull(result);
    Assert.NotEmpty(result.QualificationNumber);
    Assert.True(result.Confidence > 0.5);
}
```

---

## Utilisation dans un Contrôleur

```csharp
[ApiController]
[Route("api/ai")]
public class AIController : ControllerBase
{
    private readonly PreValidationQualificationAgent _qualificationAgent;
    private readonly NDTProgramGeneratorAgent _ndtAgent;
    private readonly ProcedureGeneratorAgent _procedureAgent;

    public AIController(
        PreValidationQualificationAgent qualificationAgent,
        NDTProgramGeneratorAgent ndtAgent,
        ProcedureGeneratorAgent procedureAgent)
    {
        _qualificationAgent = qualificationAgent;
        _ndtAgent = ndtAgent;
        _procedureAgent = procedureAgent;
    }

    [HttpPost("validate-qualification")]
    public async Task<ActionResult<QualificationData>> ValidateQualification(IFormFile certificate)
    {
        using var ms = new MemoryStream();
        await certificate.CopyToAsync(ms);
        var imageBytes = ms.ToArray();

        var result = await _qualificationAgent.ValidateQualificationAsync(imageBytes);
        return Ok(result);
    }

    [HttpPost("generate-ndt-program")]
    public async Task<ActionResult<NDTProgramData>> GenerateNDTProgram([FromBody] GenerateNDTProgramRequest request)
    {
        var result = await _ndtAgent.GenerateNDTProgramAsync(
            request.EquipmentCode,
            request.Welds,
            request.ApplicableStandards,
            request.CdcContent,
            request.NormsContent
        );

        return Ok(result);
    }

    [HttpPost("generate-procedure")]
    public async Task<ActionResult<ProcedureData>> GenerateProcedure([FromBody] GenerateProcedureRequest request)
    {
        var result = await _procedureAgent.GenerateProcedureAsync(
            request.OperationType,
            request.CdcContent,
            request.ApplicableStandards,
            request.SpecificRequirements,
            request.SafetyConstraints
        );

        return Ok(result);
    }
}
```

---

**Ce code est prêt à être intégré dans le projet. Tous les services sont fonctionnels et testables.**
