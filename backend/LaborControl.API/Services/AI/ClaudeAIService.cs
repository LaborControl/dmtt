using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

namespace LaborControl.API.Services.AI
{
    /// <summary>
    /// Claude AI Service for nuclear procedure generation and assistance
    /// Uses Anthropic Claude API
    /// </summary>
    public class ClaudeAIService : IAIService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ClaudeAIService> _logger;
        private readonly IConfiguration _configuration;
        private readonly string _apiKey;
        private readonly string _model;
        private const string CLAUDE_API_URL = "https://api.anthropic.com/v1/messages";

        public ClaudeAIService(
            HttpClient httpClient,
            ILogger<ClaudeAIService> logger,
            IConfiguration configuration)
        {
            _httpClient = httpClient;
            _logger = logger;
            _configuration = configuration;
            _apiKey = configuration["Claude:ApiKey"] ?? throw new InvalidOperationException("Claude API key not configured");
            _model = configuration["Claude:Model"] ?? "claude-3-5-sonnet-20241022";
        }

        public async Task<AIGenerationResult> GenerateDMOSAsync(DMOSGenerationRequest request)
        {
            try
            {
                var systemPrompt = @"Vous êtes un expert en soudage nucléaire et procédures de soudage.
Vous générez des DMOS (Descriptifs de Mode Opératoire de Soudage) conformes aux normes nucléaires françaises.
Respectez strictement les exigences RCC-M, EN ISO 15614, EN ISO 9606 selon le contexte.
Répondez en JSON structuré avec les sections: parametres_soudage, sequence_operations, controles_requis, criteres_acceptation.";

                var userPrompt = $@"Générez un DMOS pour les paramètres suivants:
- Procédé de soudage: {request.WeldingProcess}
- Matériaux de base: {request.BaseMaterials ?? "Non spécifié"}
- Plage d'épaisseur: {request.ThicknessRange ?? "Non spécifié"}
- Plage de diamètre: {request.DiameterRange ?? "Non spécifié"}
- Positions: {request.Positions ?? "Toutes positions"}
- Normes applicables: {request.ApplicableStandards ?? "RCC-M, EN ISO 15614"}
- Référence CDC: {request.CDCReference ?? "Non spécifié"}
- Exigences supplémentaires: {request.AdditionalRequirements ?? "Aucune"}

Générez un DMOS complet au format JSON.";

                var response = await SendClaudeRequestAsync(systemPrompt, userPrompt);

                return new AIGenerationResult
                {
                    Success = response.Success,
                    GeneratedContent = response.Content,
                    GeneratedParameters = ExtractJsonSection(response.Content, "parametres_soudage"),
                    AIModelVersion = _model,
                    PromptUsed = userPrompt,
                    Warnings = response.Warnings,
                    Error = response.Error,
                    TokensUsed = response.TokensUsed
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating DMOS");
                return new AIGenerationResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public async Task<AIGenerationResult> GenerateNDTProgramAsync(NDTProgramGenerationRequest request)
        {
            try
            {
                var systemPrompt = @"Vous êtes un expert en Contrôles Non Destructifs (CND) pour l'industrie nucléaire.
Vous générez des programmes CND conformes aux normes RCC-M, EN ISO 17637 (VT), EN ISO 3452 (PT), EN ISO 17638 (MT), EN ISO 17636 (RT), EN ISO 17640 (UT).
Définissez précisément: types de contrôles requis, séquence d'exécution, critères d'acceptation, taux d'échantillonnage.
Répondez en JSON structuré.";

                var userPrompt = $@"Générez un programme CND pour:
- Équipement: {request.AssetName ?? "Non spécifié"}
- Référence CDC: {request.CDCReference ?? "Non spécifié"}
- Procédé de soudage: {request.WeldingProcess ?? "Non spécifié"}
- Classe de soudure: {request.WeldClass ?? "Non spécifié"}
- Types de matériaux: {request.MaterialTypes ?? "Non spécifié"}
- Plage d'épaisseur: {request.ThicknessRange ?? "Non spécifié"}
- Normes applicables: {request.ApplicableStandards ?? "RCC-M"}
- Exigences: {request.AdditionalRequirements ?? "Standard nucléaire"}

Générez un programme CND complet au format JSON avec:
- required_controls: liste des types de contrôles [VT, PT, MT, RT, UT]
- control_sequence: ordre d'exécution des contrôles
- acceptance_criteria: critères par type de contrôle
- sampling_rate: taux d'échantillonnage recommandé (%)";

                var response = await SendClaudeRequestAsync(systemPrompt, userPrompt);

                return new AIGenerationResult
                {
                    Success = response.Success,
                    GeneratedContent = response.Content,
                    AIModelVersion = _model,
                    PromptUsed = userPrompt,
                    Warnings = response.Warnings,
                    Error = response.Error,
                    TokensUsed = response.TokensUsed
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating NDT program");
                return new AIGenerationResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public async Task<AIAdaptationResult> AdaptNDTProgramAsync(NDTProgramAdaptationRequest request)
        {
            try
            {
                var defectsDescription = string.Join("\n", request.DefectsFound.Select(d =>
                    $"- Type: {d.DefectType}, Location: {d.Location ?? "N/A"}, Size: {d.Size ?? "N/A"}, Control: {d.ControlType ?? "N/A"}, Weld: {d.WeldReference ?? "N/A"}"));

                var systemPrompt = @"Vous êtes un expert en Contrôles Non Destructifs (CND) pour l'industrie nucléaire.
Analysez les défauts trouvés et recommandez des adaptations au programme CND.
Justifiez chaque recommandation par des références normatives.";

                var userPrompt = $@"Le programme CND actuel a détecté les défauts suivants:
{defectsDescription}

Programme actuel:
{request.CurrentProgram ?? "Non fourni"}

Contexte additionnel: {request.AdditionalContext ?? "Aucun"}

Recommandez des adaptations au format JSON:
- recommended_changes: description des changements
- updated_required_controls: nouveaux contrôles requis
- updated_acceptance_criteria: critères modifiés
- recommended_sampling_rate: nouveau taux d'échantillonnage
- justifications: liste des justifications normatives";

                var response = await SendClaudeRequestAsync(systemPrompt, userPrompt);

                return new AIAdaptationResult
                {
                    Success = response.Success,
                    RecommendedChanges = ExtractJsonSection(response.Content, "recommended_changes"),
                    UpdatedRequiredControls = ExtractJsonSection(response.Content, "updated_required_controls"),
                    UpdatedAcceptanceCriteria = ExtractJsonSection(response.Content, "updated_acceptance_criteria"),
                    AIModelVersion = _model,
                    Error = response.Error
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adapting NDT program");
                return new AIAdaptationResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public async Task<AIPlanningResult> GetPlanningAssistanceAsync(PlanningAssistanceRequest request)
        {
            try
            {
                var weldsInfo = string.Join("\n", request.Welds.Take(50).Select(w =>
                    $"- {w.Reference}: {w.WeldingProcess}, Class: {w.WeldClass ?? "N/A"}, NDT: [{string.Join(",", w.RequiredNDTControls)}], Duration: {w.EstimatedDurationMinutes}min"));

                var systemPrompt = @"Vous êtes un expert en planification de travaux de soudage nucléaire.
Optimisez le planning en tenant compte des contraintes de ressources, séquencement des contrôles CND, et délais.
Proposez un planning réaliste et optimisé.";

                var userPrompt = $@"Planifiez les travaux suivants:
- Nombre total de soudures: {request.TotalWelds}
- Soudeurs disponibles: {request.AvailableWelders}
- Contrôleurs CND disponibles: {request.AvailableNDTControllers}
- Deadline projet: {request.ProjectDeadline:yyyy-MM-dd}
- Contraintes: {request.Constraints ?? "Aucune contrainte spécifique"}

Soudures à planifier:
{weldsInfo}

Générez un planning optimisé au format JSON avec pour chaque activité:
- weld_reference, activity_type, sequence_order, suggested_date, assigned_resource, estimated_duration_minutes, dependencies";

                var response = await SendClaudeRequestAsync(systemPrompt, userPrompt);

                return new AIPlanningResult
                {
                    Success = response.Success,
                    OptimizationNotes = response.Content,
                    AIModelVersion = _model,
                    Warnings = response.Warnings,
                    Error = response.Error
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting planning assistance");
                return new AIPlanningResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        public async Task<AIRecommendationResult> GetNCRecommendationAsync(NCRecommendationRequest request)
        {
            try
            {
                var systemPrompt = @"Vous êtes un expert qualité en soudage nucléaire.
Analysez les non-conformités et proposez des actions correctives et préventives.
Basez vos recommandations sur les bonnes pratiques et retours d'expérience du secteur nucléaire.";

                var userPrompt = $@"Analysez cette non-conformité:
- Type: {request.NCType}
- Sévérité: {request.Severity}
- Description: {request.Description}
- Défauts trouvés: {request.DefectsFound ?? "Non spécifié"}
- Cause racine identifiée: {request.RootCause ?? "À déterminer"}
- Procédé de soudage: {request.WeldingProcess ?? "Non spécifié"}
- Type de matériau: {request.MaterialType ?? "Non spécifié"}

Fournissez au format JSON:
- corrective_action: action corrective recommandée
- preventive_action: action préventive recommandée
- root_cause_analysis: analyse de la cause racine si non fournie
- similar_cases: exemples de cas similaires connus";

                var response = await SendClaudeRequestAsync(systemPrompt, userPrompt);

                return new AIRecommendationResult
                {
                    Success = response.Success,
                    CorrectiveActionRecommendation = ExtractJsonSection(response.Content, "corrective_action"),
                    PreventiveActionRecommendation = ExtractJsonSection(response.Content, "preventive_action"),
                    RootCauseAnalysis = ExtractJsonSection(response.Content, "root_cause_analysis"),
                    AIModelVersion = _model,
                    Error = response.Error
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting NC recommendation");
                return new AIRecommendationResult
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        private async Task<ClaudeResponse> SendClaudeRequestAsync(string systemPrompt, string userPrompt)
        {
            try
            {
                var requestBody = new
                {
                    model = _model,
                    max_tokens = 4096,
                    system = systemPrompt,
                    messages = new[]
                    {
                        new { role = "user", content = userPrompt }
                    }
                };

                var request = new HttpRequestMessage(HttpMethod.Post, CLAUDE_API_URL);
                request.Headers.Add("x-api-key", _apiKey);
                request.Headers.Add("anthropic-version", "2023-06-01");
                request.Content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    Encoding.UTF8,
                    "application/json");

                var response = await _httpClient.SendAsync(request);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Claude API error: {StatusCode} - {Response}", response.StatusCode, responseContent);
                    return new ClaudeResponse
                    {
                        Success = false,
                        Error = $"API Error: {response.StatusCode}"
                    };
                }

                using var doc = JsonDocument.Parse(responseContent);
                var content = doc.RootElement
                    .GetProperty("content")[0]
                    .GetProperty("text")
                    .GetString();

                var usage = doc.RootElement.GetProperty("usage");
                var tokensUsed = usage.GetProperty("input_tokens").GetInt32() +
                                usage.GetProperty("output_tokens").GetInt32();

                return new ClaudeResponse
                {
                    Success = true,
                    Content = content,
                    TokensUsed = tokensUsed
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error calling Claude API");
                return new ClaudeResponse
                {
                    Success = false,
                    Error = ex.Message
                };
            }
        }

        private string? ExtractJsonSection(string? content, string sectionName)
        {
            if (string.IsNullOrEmpty(content)) return null;

            try
            {
                // Try to parse as JSON and extract section
                using var doc = JsonDocument.Parse(content);
                if (doc.RootElement.TryGetProperty(sectionName, out var section))
                {
                    return section.GetRawText();
                }
            }
            catch
            {
                // Content is not valid JSON, return as-is
            }

            return null;
        }

        private class ClaudeResponse
        {
            public bool Success { get; set; }
            public string? Content { get; set; }
            public string? Error { get; set; }
            public List<string> Warnings { get; set; } = new();
            public int TokensUsed { get; set; }
        }
    }
}
