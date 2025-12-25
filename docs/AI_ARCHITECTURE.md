# Architecture Multi-IA - Claude + Gemini

**LABOR CONTROL DMTT**
Date : 24 décembre 2025

## Vue d'Ensemble

Le projet utilise **deux moteurs IA** complémentaires pour maximiser les performances :

1. **Claude (Anthropic)** - Génération de texte structuré, procédures, analyse de documents
2. **Gemini (Google)** - Vision multimodale, extraction de données depuis PDF/images

## Stratégie de Répartition

### Claude (Anthropic API)
**Modèle** : `claude-3-5-sonnet-20241022` (ou plus récent)

**Utilisé pour** :
- ✅ Génération de programmes CND (texte structuré long)
- ✅ Génération de procédures techniques (logique complexe)
- ✅ Adaptation de programmes CND après FNC (raisonnement)
- ✅ Génération de plans de correction
- ✅ Analyse de conformité CDC/normes

**Pourquoi Claude** :
- Excellent pour le raisonnement complexe
- Contexte long (200k tokens)
- Structured output natif
- Très bon sur le français technique
- Meilleur pour la génération de procédures détaillées

### Gemini (Google AI)
**Modèle** : `gemini-2.0-flash-exp` ou `gemini-1.5-pro`

**Utilisé pour** :
- ✅ Pré-validation qualifications (OCR + extraction PDF/images)
- ✅ Extraction de données depuis certificats matériaux
- ✅ Analyse de photos de contrôle CND
- ✅ Détection de défauts visuels sur soudures
- ✅ Extraction de données depuis plans BE (DWG → PDF)

**Pourquoi Gemini** :
- Vision multimodale native
- Excellent OCR (meilleur que GPT-4V)
- Gratuit en usage modéré (API Gemini)
- Très rapide (Flash)
- Bon rapport qualité/prix

## Architecture des Services

### Service Factory Pattern

```
AIServiceFactory
├── ClaudeService (génération texte/procédures)
├── GeminiService (vision/extraction)
└── AIOrchestrator (routage intelligent)
```

### Diagramme de Flux

```
┌─────────────────────────────────────────────────────────────┐
│                    LABOR CONTROL DMTT                       │
└─────────────────────────────────────────────────────────────┘
                              │
                              ▼
                  ┌───────────────────────┐
                  │   AI Orchestrator     │
                  │  (Routage intelligent)│
                  └───────────────────────┘
                              │
                 ┌────────────┴────────────┐
                 ▼                         ▼
        ┌─────────────────┐      ┌─────────────────┐
        │  Claude Service │      │  Gemini Service │
        │   (Anthropic)   │      │    (Google)     │
        └─────────────────┘      └─────────────────┘
                 │                         │
                 ▼                         ▼
    ┌─────────────────────────┐  ┌──────────────────────┐
    │ - Programmes CND        │  │ - OCR Certificats    │
    │ - Procédures            │  │ - Extraction données │
    │ - Adaptations FNC       │  │ - Analyse photos     │
    │ - Conformité CDC        │  │ - Détection défauts  │
    └─────────────────────────┘  └──────────────────────┘
```

## Agents IA - Répartition

### Agent 1 : PreValidationQualificationAgent
**IA** : 🟢 **Gemini 2.0 Flash**

**Raison** : Vision multimodale + OCR excellence

**Input** : PDF ou image de certificat de qualification

**Process** :
```
1. Upload fichier vers Blob Storage
2. Appel Gemini avec prompt extraction
3. OCR + extraction données structurées
4. Validation format + cohérence
5. Retour JSON structuré
```

**Output** :
```json
{
  "qualificationNumber": "CERT-WLD-2024-12345",
  "holderName": "Jean DUPONT",
  "weldingProcess": ["TIG", "MIG"],
  "materials": "Acier inox 304L, 316L",
  "thicknessRange": "3-12mm",
  "diameterRange": "DN50-DN300",
  "issueDate": "2024-01-15",
  "expirationDate": "2027-01-15",
  "issuingBody": "Bureau Veritas",
  "confidence": 0.96,
  "warnings": []
}
```

**Endpoint** : `POST /api/ai/validate-qualification`

---

### Agent 2 : NDTProgramGeneratorAgent
**IA** : 🔵 **Claude 3.5 Sonnet**

**Raison** : Génération de texte structuré long avec raisonnement complexe

**Input** :
```json
{
  "equipmentId": 123,
  "equipmentCode": "EQP-TRI-001",
  "welds": [
    {
      "reference": "S-001",
      "diameter": "DN100",
      "thickness": "6mm",
      "weldClass": "B",
      "material1": "316L",
      "material2": "316L",
      "weldingProcess": "TIG"
    }
  ],
  "applicableStandards": ["RCC-M", "EN ISO 17640", "EN ISO 3452"],
  "cdcReference": "CDC-ORANO-2024-TRICASTIN-001"
}
```

**Process** :
```
1. Récupérer CDC depuis Blob Storage
2. Récupérer normes EDF depuis serveur
3. Construire prompt structuré pour Claude
4. Appel Claude API avec context complet
5. Parse réponse structurée (JSON mode)
6. Génération PDF avec QuestPDF
7. Upload PDF vers Blob Storage
8. Retour référence + métadonnées
```

**Prompt Claude** :
```xml
<role>
Tu es un expert en contrôles non destructifs (CND) pour le secteur nucléaire français.
Tu connais parfaitement les normes RCC-M, RSEM, et les standards EDF.
</role>

<task>
Génère un programme de contrôle non destructif (CND) complet et conforme pour les soudures suivantes.
Le programme doit être conforme au CDC ORANO et aux normes EDF applicables.
</task>

<context>
<equipment>
Code: {{equipmentCode}}
Système: {{system}}
</equipment>

<welds>
{{welds_json}}
</welds>

<cdc>
{{cdc_content}}
</cdc>

<standards>
{{standards_content}}
</standards>
</context>

<instructions>
Pour chaque soudure, détermine :
1. Les contrôles CND requis (VT, PT, MT, RT, UT)
2. L'ordre des contrôles (séquence)
3. Les normes applicables pour chaque contrôle
4. Les critères d'acceptation (niveau A, B, ou C selon RCC-M)
5. Le pourcentage de contrôle (100%, échantillonnage)
6. Les exigences spécifiques (accessibilité, préparation surface)

Format de sortie : JSON structuré
</instructions>

<output_format>
{
  "programReference": "PROG-CND-XXX",
  "equipmentCode": "...",
  "creationDate": "2025-01-15",
  "applicableStandards": ["RCC-M", "..."],
  "controls": [
    {
      "weldReference": "S-001",
      "sequence": 1,
      "controlType": "VT",
      "standard": "EN ISO 17637",
      "acceptanceCriteria": "Niveau B selon RCC-M",
      "coverage": "100%",
      "timing": "Pendant et après soudage",
      "specificRequirements": "Éclairage ≥ 500 lux"
    }
  ],
  "generalRequirements": "...",
  "safetyRequirements": "Zone contrôlée - Habilitation nucléaire requise"
}
</output_format>
```

**Output** :
```json
{
  "programReference": "PROG-CND-001-2025",
  "filePath": "https://stlaborcontroldmtt.blob.../ndt-programs/PROG-CND-001.pdf",
  "equipmentCode": "EQP-TRI-001",
  "createdAt": "2025-01-15T10:30:00Z",
  "createdByAI": true,
  "aiModel": "claude-3-5-sonnet-20241022",
  "controls": [
    {
      "weldReference": "S-001",
      "controlType": "VT",
      "standard": "EN ISO 17637",
      "acceptanceCriteria": "Niveau B",
      "coverage": "100%",
      "timing": "Pendant et après soudage"
    },
    {
      "weldReference": "S-001",
      "controlType": "PT",
      "standard": "EN ISO 3452-1",
      "acceptanceCriteria": "Niveau 2",
      "coverage": "100%",
      "timing": "Après soudage et meulage"
    }
  ]
}
```

**Endpoint** : `POST /api/ai/generate-ndt-program`

---

### Agent 3 : ProcedureGeneratorAgent
**IA** : 🔵 **Claude 3.5 Sonnet**

**Raison** : Génération de procédures détaillées avec logique complexe

**Input** :
```json
{
  "operationType": "Soudage TIG acier inoxydable",
  "equipmentType": "Tuyauterie DN100",
  "cdcReference": "CDC-ORANO-2024-TRICASTIN-001",
  "applicableStandards": ["RCC-M", "EN 1090-2", "NF A89-100"],
  "specificRequirements": [
    "Zone nucléaire contrôlée",
    "Procédé qualifié DMOS-TIG-001",
    "Traçabilité complète"
  ],
  "safetyConstraints": [
    "Habilitation nucléaire requise",
    "Contrôle contamination avant/après"
  ]
}
```

**Process** :
```
1. Récupérer CDC depuis Blob
2. Récupérer normes depuis serveur
3. Appel Claude avec prompt procédure
4. Parse réponse structurée
5. Génération PDF avec QuestPDF
6. Upload Blob Storage
7. Retour référence
```

**Prompt Claude** :
```xml
<role>
Tu es un expert en soudage nucléaire et rédaction de procédures techniques.
Tu connais les normes RCC-M, RSEM, COFREND, et les exigences EDF.
</role>

<task>
Génère une procédure technique complète et opérationnelle pour l'opération suivante :
{{operationType}}

La procédure doit être conforme au CDC ORANO et aux normes applicables.
Elle doit être utilisable directement sur le terrain par les opérateurs.
</task>

<context>
<cdc>{{cdc_content}}</cdc>
<standards>{{standards_content}}</standards>
<requirements>{{specific_requirements}}</requirements>
<safety>{{safety_constraints}}</safety>
</context>

<instructions>
Structure la procédure avec les sections suivantes :

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
15. ANNEXES (si nécessaire)

Chaque étape d'exécution doit être :
- Numérotée
- Action précise (verbe à l'infinitif)
- Paramètres techniques (si applicable)
- Point de contrôle (si requis)
- Critère d'acceptation (si applicable)

Format : JSON structuré
</instructions>
```

**Output** :
```json
{
  "procedureReference": "PROC-WELD-TIG-INOX-001",
  "version": "1.0",
  "filePath": "https://stlaborcontroldmtt.blob.../procedures/PROC-WELD-TIG-INOX-001.pdf",
  "operationType": "Soudage TIG acier inoxydable",
  "createdAt": "2025-01-15T14:20:00Z",
  "createdByAI": true,
  "aiModel": "claude-3-5-sonnet-20241022",
  "sections": [
    {
      "number": "8",
      "title": "PRÉPARATION",
      "subsections": [
        {
          "title": "Vérifications préalables",
          "steps": [
            "Vérifier la qualification du soudeur pour le procédé TIG",
            "Vérifier la validité du DMOS applicable",
            "Contrôler la conformité des matériaux (certificats matières)"
          ]
        }
      ]
    },
    {
      "number": "9",
      "title": "EXÉCUTION",
      "steps": [
        {
          "number": "9.1",
          "action": "Préparer le chanfrein selon plan BE",
          "parameters": "Angle 30° ± 2°, jeu 2mm ± 0.5mm",
          "controlPoint": true,
          "acceptanceCriteria": "Dimensions conformes au plan"
        }
      ]
    }
  ]
}
```

**Endpoint** : `POST /api/ai/generate-procedure`

---

### Agent 4 : MaterialCertificateExtractorAgent (BONUS)
**IA** : 🟢 **Gemini 2.0 Flash**

**Raison** : OCR + extraction données techniques

**Input** : PDF certificat matériau (3.1, 3.2 EN 10204)

**Output** :
```json
{
  "certificateType": "3.1",
  "standard": "EN 10204",
  "material": {
    "designation": "X6CrNiTi18-10",
    "grade": "1.4541",
    "heatNumber": "AB123456",
    "supplier": "ArcelorMittal",
    "dimensions": "Tube Ø114.3 x 3.6mm"
  },
  "chemicalComposition": {
    "C": 0.06,
    "Cr": 18.2,
    "Ni": 10.1,
    "Ti": 0.45
  },
  "mechanicalProperties": {
    "tensileStrength": 580,
    "yieldStrength": 240,
    "elongation": 45
  },
  "conformity": true,
  "confidence": 0.94
}
```

**Endpoint** : `POST /api/ai/extract-material-certificate`

---

### Agent 5 : DefectAnalysisAgent (BONUS)
**IA** : 🟢 **Gemini 1.5 Pro**

**Raison** : Vision avancée pour détection défauts

**Input** : Photo de soudure ou résultat CND (RT, PT)

**Output** :
```json
{
  "defectsDetected": [
    {
      "type": "Porosité",
      "location": "Passe racine, 45mm depuis origine",
      "severity": "Mineur",
      "dimensions": "Ø 2mm",
      "acceptability": "Acceptable selon RCC-M niveau B",
      "confidence": 0.89
    }
  ],
  "overallConformity": "Conforme",
  "recommendation": "Aucune action requise"
}
```

**Endpoint** : `POST /api/ai/analyze-defect`

---

## Implémentation Backend

### Structure des Services

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
    ├── ProcedureGeneratorAgent.cs
    ├── MaterialCertificateExtractorAgent.cs
    └── DefectAnalysisAgent.cs
```

### Code : IAIService.cs

```csharp
public interface IAIService
{
    Task<string> GenerateTextAsync(string prompt, object? context = null);
    Task<T> GenerateStructuredAsync<T>(string prompt, object? context = null);
    Task<string> AnalyzeImageAsync(string prompt, byte[] imageData);
    Task<T> ExtractDataFromImageAsync<T>(string prompt, byte[] imageData);
}
```

### Code : ClaudeService.cs

```csharp
using Anthropic.SDK;
using Anthropic.SDK.Messaging;

public class ClaudeService : IClaudeService
{
    private readonly AnthropicClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<ClaudeService> _logger;
    private const string MODEL = "claude-3-5-sonnet-20241022";

    public ClaudeService(IConfiguration configuration, ILogger<ClaudeService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        var apiKey = _configuration["Claude:ApiKey"];
        _client = new AnthropicClient(apiKey);
    }

    public async Task<string> GenerateTextAsync(string prompt, object? context = null)
    {
        try
        {
            var messages = new List<Message>
            {
                new Message
                {
                    Role = "user",
                    Content = context != null
                        ? $"{prompt}\n\nContext:\n{JsonSerializer.Serialize(context)}"
                        : prompt
                }
            };

            var parameters = new MessageParameters
            {
                Messages = messages,
                Model = MODEL,
                MaxTokens = 4096,
                Temperature = 0.3m
            };

            var response = await _client.Messages.GetClaudeMessageAsync(parameters);
            return response.Content.First().Text;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'appel à Claude API");
            throw;
        }
    }

    public async Task<T> GenerateStructuredAsync<T>(string prompt, object? context = null)
    {
        var systemPrompt = $@"Tu dois répondre uniquement avec un JSON valide correspondant à cette structure :
{typeof(T).Name}

Ne génère AUCUN texte avant ou après le JSON. Uniquement le JSON brut.";

        var response = await GenerateTextAsync($"{systemPrompt}\n\n{prompt}", context);

        // Parse JSON
        return JsonSerializer.Deserialize<T>(response)
            ?? throw new Exception("Impossible de parser la réponse en JSON");
    }
}
```

### Code : GeminiService.cs

```csharp
using Google.Cloud.AIPlatform.V1;
using Google.Api.Gax.Grpc;

public class GeminiService : IGeminiService
{
    private readonly PredictionServiceClient _client;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiService> _logger;
    private const string MODEL = "gemini-2.0-flash-exp";
    private readonly string _projectId;
    private readonly string _location;

    public GeminiService(IConfiguration configuration, ILogger<GeminiService> logger)
    {
        _configuration = configuration;
        _logger = logger;

        _projectId = _configuration["Gemini:ProjectId"] ?? throw new Exception("Gemini ProjectId manquant");
        _location = _configuration["Gemini:Location"] ?? "us-central1";

        var clientBuilder = new PredictionServiceClientBuilder
        {
            Endpoint = $"{_location}-aiplatform.googleapis.com"
        };

        _client = clientBuilder.Build();
    }

    public async Task<T> ExtractDataFromImageAsync<T>(string prompt, byte[] imageData)
    {
        try
        {
            var endpoint = $"projects/{_projectId}/locations/{_location}/publishers/google/models/{MODEL}";

            // Convertir image en base64
            var base64Image = Convert.ToBase64String(imageData);

            // Construire la requête
            var systemInstruction = $@"Tu es un expert en extraction de données depuis des documents techniques.
Extrais les informations demandées et retourne uniquement un JSON valide.
Structure attendue : {typeof(T).Name}";

            var content = new
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

            var jsonRequest = JsonSerializer.Serialize(content);

            // Appel API (version REST simplifiée)
            using var httpClient = new HttpClient();
            var apiKey = _configuration["Gemini:ApiKey"];
            var url = $"https://generativelanguage.googleapis.com/v1beta/models/{MODEL}:generateContent?key={apiKey}";

            var httpContent = new StringContent(jsonRequest, Encoding.UTF8, "application/json");
            var response = await httpClient.PostAsync(url, httpContent);
            response.EnsureSuccessStatusCode();

            var responseJson = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<GeminiResponse>(responseJson);

            var generatedText = result?.Candidates?.FirstOrDefault()?.Content?.Parts?.FirstOrDefault()?.Text
                ?? throw new Exception("Pas de texte généré par Gemini");

            // Parse JSON
            return JsonSerializer.Deserialize<T>(generatedText)
                ?? throw new Exception("Impossible de parser la réponse Gemini");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erreur lors de l'appel à Gemini API");
            throw;
        }
    }

    public async Task<string> AnalyzeImageAsync(string prompt, byte[] imageData)
    {
        var result = await ExtractDataFromImageAsync<Dictionary<string, object>>(prompt, imageData);
        return JsonSerializer.Serialize(result);
    }

    // Classe pour parser la réponse Gemini
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

### Code : AIOrchestrator.cs

```csharp
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

    public async Task<T> RouteRequest<T>(AIRequest request)
    {
        _logger.LogInformation($"Routing AI request: {request.Type}");

        return request.Type switch
        {
            AIRequestType.ExtractFromImage =>
                await _geminiService.ExtractDataFromImageAsync<T>(request.Prompt, request.ImageData!),

            AIRequestType.GenerateStructuredText =>
                await _claudeService.GenerateStructuredAsync<T>(request.Prompt, request.Context),

            AIRequestType.AnalyzeImage =>
                (T)(object)await _geminiService.AnalyzeImageAsync(request.Prompt, request.ImageData!),

            AIRequestType.GenerateText =>
                (T)(object)await _claudeService.GenerateTextAsync(request.Prompt, request.Context),

            _ => throw new ArgumentException($"Type de requête non supporté: {request.Type}")
        };
    }
}

public class AIRequest
{
    public AIRequestType Type { get; set; }
    public string Prompt { get; set; } = string.Empty;
    public object? Context { get; set; }
    public byte[]? ImageData { get; set; }
}

public enum AIRequestType
{
    GenerateText,
    GenerateStructuredText,
    AnalyzeImage,
    ExtractFromImage
}
```

## Configuration

### appsettings.json

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
    "ProjectId": "your-gcp-project-id",
    "Location": "us-central1",
    "Model": "gemini-2.0-flash-exp",
    "MaxTokens": 2048,
    "Temperature": 0.2
  }
}
```

### Azure Key Vault (Production)

```bash
# Stockage sécurisé des clés API
az keyvault secret set --vault-name kv-laborcontrol-dmtt --name "Claude-ApiKey" --value "sk-ant-..."
az keyvault secret set --vault-name kv-laborcontrol-dmtt --name "Gemini-ApiKey" --value "AIza..."
```

## Packages NuGet Requis

```xml
<!-- Claude (Anthropic SDK) -->
<PackageReference Include="Anthropic.SDK" Version="0.2.0" />

<!-- Google Cloud AI Platform -->
<PackageReference Include="Google.Cloud.AIPlatform.V1" Version="3.0.0" />
<PackageReference Include="Google.Apis.Auth" Version="1.68.0" />
```

Ou utiliser les API REST directement (recommandé pour plus de flexibilité).

## Coûts Estimés

### Claude (Anthropic)
**Modèle** : claude-3-5-sonnet-20241022

- Input : $3 / 1M tokens
- Output : $15 / 1M tokens

**Estimation mensuelle** (500 générations/mois) :
- Prompt moyen : 5000 tokens
- Réponse moyenne : 2000 tokens
- Coût : (500 × 5000 × $3 / 1M) + (500 × 2000 × $15 / 1M) = **$22.50 / mois**

### Gemini (Google AI)
**Modèle** : gemini-2.0-flash-exp

- **GRATUIT** jusqu'à 1500 requêtes/jour (Flash)
- Gemini 1.5 Pro : $1.25 / 1M tokens (input), $5 / 1M tokens (output)

**Estimation mensuelle** (1000 extractions/mois avec Flash) :
- **GRATUIT** (sous la limite)

**Total estimé : ~$25/mois** (très économique)

## Tests & Validation

### Test Agent 1 (Gemini)
```bash
POST /api/ai/validate-qualification
Content-Type: multipart/form-data

file: certificat-soudeur.pdf
```

### Test Agent 2 (Claude)
```bash
POST /api/ai/generate-ndt-program
Content-Type: application/json

{
  "equipmentId": 1,
  "welds": [...],
  "applicableStandards": ["RCC-M"]
}
```

### Test Agent 3 (Claude)
```bash
POST /api/ai/generate-procedure
Content-Type: application/json

{
  "operationType": "Soudage TIG",
  "cdcReference": "CDC-001"
}
```

## Monitoring & Logs

### Métriques à Suivre
- Nombre d'appels Claude vs Gemini
- Temps de réponse moyen
- Taux d'erreur par provider
- Coût journalier/mensuel
- Qualité des outputs (feedback utilisateur)

### Application Insights
```csharp
_logger.LogInformation("Claude API call", new
{
    Model = "claude-3-5-sonnet",
    InputTokens = 5000,
    OutputTokens = 2000,
    Latency = 3.5,
    Cost = 0.045
});
```

## Avantages de cette Architecture

### ✅ Flexibilité
- Meilleur modèle pour chaque tâche
- Fallback possible (si Claude down → Gemini)

### ✅ Coût Optimisé
- Gemini gratuit pour OCR/extraction
- Claude uniquement pour génération complexe

### ✅ Performance
- Gemini Flash très rapide (<1s)
- Claude excellent sur raisonnement

### ✅ Qualité
- OCR Gemini > GPT-4V
- Génération procédures Claude > autres

### ✅ Indépendance
- Pas de vendor lock-in Azure
- APIs standard (REST)

## Prochaines Étapes

1. **Sprint 3** : Implémenter les 5 services IA
2. **Tests unitaires** : Mock des APIs pour tests
3. **Tests d'intégration** : Vrais appels avec fichiers test
4. **Optimisation prompts** : Itération sur qualité
5. **Monitoring** : Dashboards coûts/performance

---

**Cette architecture multi-IA optimise les coûts (~$25/mois) tout en maximisant la qualité des outputs.**
