namespace LaborControl.API.Services.AI
{
    /// <summary>
    /// Interface for AI service integration (Claude/Gemini)
    /// </summary>
    public interface IAIService
    {
        /// <summary>
        /// Generate DMOS (Welding Procedure Specification) using Claude
        /// </summary>
        Task<AIGenerationResult> GenerateDMOSAsync(DMOSGenerationRequest request);

        /// <summary>
        /// Generate NDT Program using Claude
        /// </summary>
        Task<AIGenerationResult> GenerateNDTProgramAsync(NDTProgramGenerationRequest request);

        /// <summary>
        /// Adapt NDT Program based on defects found
        /// </summary>
        Task<AIAdaptationResult> AdaptNDTProgramAsync(NDTProgramAdaptationRequest request);

        /// <summary>
        /// Provide planning assistance using Claude
        /// </summary>
        Task<AIPlanningResult> GetPlanningAssistanceAsync(PlanningAssistanceRequest request);

        /// <summary>
        /// Get AI recommendations for non-conformity resolution
        /// </summary>
        Task<AIRecommendationResult> GetNCRecommendationAsync(NCRecommendationRequest request);
    }

    /// <summary>
    /// Interface for Gemini OCR service (qualification document analysis)
    /// </summary>
    public interface IGeminiOCRService
    {
        /// <summary>
        /// Pre-validate welder/NDT qualification document using Gemini Vision
        /// </summary>
        Task<QualificationPreValidationResult> PreValidateQualificationAsync(QualificationPreValidationRequest request);

        /// <summary>
        /// Extract metadata from technical document
        /// </summary>
        Task<DocumentExtractionResult> ExtractDocumentMetadataAsync(DocumentExtractionRequest request);
    }

    #region Request/Response Models

    public class DMOSGenerationRequest
    {
        public string WeldingProcess { get; set; } = string.Empty;
        public string? BaseMaterials { get; set; }
        public string? ThicknessRange { get; set; }
        public string? DiameterRange { get; set; }
        public string? Positions { get; set; }
        public string? AdditionalRequirements { get; set; }
        public string? CDCReference { get; set; }
        public string? ApplicableStandards { get; set; }
    }

    public class NDTProgramGenerationRequest
    {
        public Guid? AssetId { get; set; }
        public string? AssetName { get; set; }
        public string? CDCReference { get; set; }
        public string? WeldingProcess { get; set; }
        public string? WeldClass { get; set; }
        public string? MaterialTypes { get; set; }
        public string? ThicknessRange { get; set; }
        public string? ApplicableStandards { get; set; }
        public string? AdditionalRequirements { get; set; }
    }

    public class NDTProgramAdaptationRequest
    {
        public Guid ProgramId { get; set; }
        public string? CurrentProgram { get; set; }
        public List<DefectInfoRequest> DefectsFound { get; set; } = new();
        public string? AdditionalContext { get; set; }
    }

    public class DefectInfoRequest
    {
        public string DefectType { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? Size { get; set; }
        public string? ControlType { get; set; }
        public string? WeldReference { get; set; }
    }

    public class PlanningAssistanceRequest
    {
        public int TotalWelds { get; set; }
        public int AvailableWelders { get; set; }
        public int AvailableNDTControllers { get; set; }
        public DateTime ProjectDeadline { get; set; }
        public List<WeldPlanningInfo> Welds { get; set; } = new();
        public string? Constraints { get; set; }
    }

    public class WeldPlanningInfo
    {
        public string Reference { get; set; } = string.Empty;
        public string WeldingProcess { get; set; } = string.Empty;
        public string? WeldClass { get; set; }
        public List<string> RequiredNDTControls { get; set; } = new();
        public int EstimatedDurationMinutes { get; set; }
        public List<string> Prerequisites { get; set; } = new();
    }

    public class NCRecommendationRequest
    {
        public string NCType { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string? DefectsFound { get; set; }
        public string? RootCause { get; set; }
        public string? WeldingProcess { get; set; }
        public string? MaterialType { get; set; }
    }

    public class QualificationPreValidationRequest
    {
        public string DocumentBase64 { get; set; } = string.Empty;
        public string DocumentMimeType { get; set; } = string.Empty;
        public string QualificationType { get; set; } = string.Empty;
        public string? ExpectedStandard { get; set; }
    }

    public class DocumentExtractionRequest
    {
        public string DocumentBase64 { get; set; } = string.Empty;
        public string DocumentMimeType { get; set; } = string.Empty;
        public string DocumentType { get; set; } = string.Empty;
        public bool ExtractText { get; set; } = true;
        public bool ExtractMetadata { get; set; } = true;
    }

    // Results

    public class AIGenerationResult
    {
        public bool Success { get; set; }
        public string? GeneratedContent { get; set; }
        public string? GeneratedParameters { get; set; }
        public string? AIModelVersion { get; set; }
        public string? PromptUsed { get; set; }
        public List<string> Warnings { get; set; } = new();
        public string? Error { get; set; }
        public int TokensUsed { get; set; }
    }

    public class AIAdaptationResult
    {
        public bool Success { get; set; }
        public string? RecommendedChanges { get; set; }
        public string? UpdatedRequiredControls { get; set; }
        public string? UpdatedAcceptanceCriteria { get; set; }
        public int? RecommendedSamplingRate { get; set; }
        public List<string> Justifications { get; set; } = new();
        public string? AIModelVersion { get; set; }
        public string? Error { get; set; }
    }

    public class AIPlanningResult
    {
        public bool Success { get; set; }
        public List<PlannedActivity> SuggestedPlan { get; set; } = new();
        public string? OptimizationNotes { get; set; }
        public List<string> Warnings { get; set; } = new();
        public string? AIModelVersion { get; set; }
        public string? Error { get; set; }
    }

    public class PlannedActivity
    {
        public string WeldReference { get; set; } = string.Empty;
        public string ActivityType { get; set; } = string.Empty; // WELD, NDT_VT, NDT_PT, etc.
        public int SequenceOrder { get; set; }
        public DateTime SuggestedDate { get; set; }
        public string? AssignedResource { get; set; }
        public int EstimatedDurationMinutes { get; set; }
        public List<string> Dependencies { get; set; } = new();
    }

    public class AIRecommendationResult
    {
        public bool Success { get; set; }
        public string? CorrectiveActionRecommendation { get; set; }
        public string? PreventiveActionRecommendation { get; set; }
        public string? RootCauseAnalysis { get; set; }
        public List<string> SimilarCases { get; set; } = new();
        public string? AIModelVersion { get; set; }
        public string? Error { get; set; }
    }

    public class QualificationPreValidationResult
    {
        public bool Success { get; set; }
        public decimal ConfidenceScore { get; set; }
        public QualificationExtractedData? ExtractedData { get; set; }
        public List<string> Warnings { get; set; } = new();
        public List<string> ValidationIssues { get; set; } = new();
        public string? AIModelVersion { get; set; }
        public string? Error { get; set; }
    }

    public class QualificationExtractedData
    {
        public string? QualificationNumber { get; set; }
        public string? HolderName { get; set; }
        public string? QualificationType { get; set; }
        public string? WeldingProcess { get; set; }
        public int? CertificationLevel { get; set; }
        public string? QualifiedMaterials { get; set; }
        public string? ThicknessRange { get; set; }
        public string? DiameterRange { get; set; }
        public string? QualifiedPositions { get; set; }
        public string? QualificationStandard { get; set; }
        public string? CertifyingBody { get; set; }
        public DateTime? IssueDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
    }

    public class DocumentExtractionResult
    {
        public bool Success { get; set; }
        public string? ExtractedText { get; set; }
        public Dictionary<string, string> ExtractedMetadata { get; set; } = new();
        public string? DocumentSummary { get; set; }
        public List<string> DetectedKeywords { get; set; } = new();
        public string? AIModelVersion { get; set; }
        public string? Error { get; set; }
    }

    #endregion
}
