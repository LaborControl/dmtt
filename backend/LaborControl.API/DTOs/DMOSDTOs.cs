namespace LaborControl.API.DTOs
{
    /// <summary>
    /// DTO for DMOS list display
    /// </summary>
    public class DMOSDto
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string WeldingProcess { get; set; } = string.Empty;
        public string? BaseMaterials { get; set; }
        public string? ThicknessRange { get; set; }
        public string? DiameterRange { get; set; }
        public string? QualifiedPositions { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool GeneratedByAI { get; set; }
        public Guid? ApprovedById { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public DateTime? ValidUntil { get; set; }
        public bool IsExpired => ValidUntil.HasValue && ValidUntil.Value < DateTime.UtcNow;
        public int WeldsCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO for DMOS detail view
    /// </summary>
    public class DMOSDetailDto : DMOSDto
    {
        public string? FillerMetal { get; set; }
        public string? ShieldingGas { get; set; }
        public string? WeldingParameters { get; set; }
        public string? ApplicableStandards { get; set; }
        public string? FilePath { get; set; }
        public string? AIModelVersion { get; set; }
        public string? AIPrompt { get; set; }
        public string? ApprovalComments { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<WeldDto> Welds { get; set; } = new();
    }

    /// <summary>
    /// Request to create a new DMOS
    /// </summary>
    public class CreateDMOSRequest
    {
        public string Reference { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0";
        public string WeldingProcess { get; set; } = "TIG";
        public string? BaseMaterials { get; set; }
        public string? ThicknessRange { get; set; }
        public string? DiameterRange { get; set; }
        public string? QualifiedPositions { get; set; }
        public string? FillerMetal { get; set; }
        public string? ShieldingGas { get; set; }
        public string? WeldingParameters { get; set; }
        public string? ApplicableStandards { get; set; }
        public DateTime? ValidUntil { get; set; }
    }

    /// <summary>
    /// Request to update a DMOS
    /// </summary>
    public class UpdateDMOSRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0";
        public string WeldingProcess { get; set; } = "TIG";
        public string? BaseMaterials { get; set; }
        public string? ThicknessRange { get; set; }
        public string? DiameterRange { get; set; }
        public string? QualifiedPositions { get; set; }
        public string? FillerMetal { get; set; }
        public string? ShieldingGas { get; set; }
        public string? WeldingParameters { get; set; }
        public string? ApplicableStandards { get; set; }
        public DateTime? ValidUntil { get; set; }
        public string Status { get; set; } = "DRAFT";
    }

    /// <summary>
    /// Request for DMOS approval
    /// </summary>
    public class ApproveDMOSRequest
    {
        public bool Approve { get; set; } = true;
        public string? Comments { get; set; }
    }

    /// <summary>
    /// Request to upload DMOS file
    /// </summary>
    public class DMOSFileUploadRequest
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string Base64Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request for AI DMOS generation
    /// </summary>
    public class GenerateDMOSRequest
    {
        public string WeldingProcess { get; set; } = "TIG";
        public string? BaseMaterials { get; set; }
        public string? ThicknessRange { get; set; }
        public string? DiameterRange { get; set; }
        public string? Positions { get; set; }
        public string? AdditionalRequirements { get; set; }
        public string? CDCReference { get; set; }
    }

    /// <summary>
    /// Result of AI DMOS generation
    /// </summary>
    public class GenerateDMOSResultDto
    {
        public bool Success { get; set; }
        public string? GeneratedContent { get; set; }
        public string? GeneratedParameters { get; set; }
        public string? AIModelVersion { get; set; }
        public string? PromptUsed { get; set; }
        public List<string> Warnings { get; set; } = new();
        public Guid? CreatedDMOSId { get; set; }
    }
}
