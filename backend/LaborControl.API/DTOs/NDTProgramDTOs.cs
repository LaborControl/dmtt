namespace LaborControl.API.DTOs
{
    /// <summary>
    /// DTO for NDT Program list display
    /// </summary>
    public class NDTProgramDto
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public Guid? AssetId { get; set; }
        public string? AssetName { get; set; }
        public string? RequiredControls { get; set; }
        public string? CDCReference { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool GeneratedByAI { get; set; }
        public Guid? ApprovedById { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public int SamplingRate { get; set; }
        public int ControlsCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO for NDT Program detail view
    /// </summary>
    public class NDTProgramDetailDto : NDTProgramDto
    {
        public string? AcceptanceCriteria { get; set; }
        public string? ApplicableStandards { get; set; }
        public string? FilePath { get; set; }
        public string? ControlSequence { get; set; }
        public string? AIModelVersion { get; set; }
        public string? AIPrompt { get; set; }
        public string? AIInputData { get; set; }
        public string? ApprovalComments { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<NDTControlDto> Controls { get; set; } = new();
    }

    /// <summary>
    /// Request to create a new NDT Program
    /// </summary>
    public class CreateNDTProgramRequest
    {
        public string Reference { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0";
        public Guid? AssetId { get; set; }
        public string? RequiredControls { get; set; }
        public string? AcceptanceCriteria { get; set; }
        public string? ApplicableStandards { get; set; }
        public string? CDCReference { get; set; }
        public int SamplingRate { get; set; } = 100;
        public string? ControlSequence { get; set; }
    }

    /// <summary>
    /// Request to update an NDT Program
    /// </summary>
    public class UpdateNDTProgramRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = "1.0";
        public Guid? AssetId { get; set; }
        public string? RequiredControls { get; set; }
        public string? AcceptanceCriteria { get; set; }
        public string? ApplicableStandards { get; set; }
        public string? CDCReference { get; set; }
        public int SamplingRate { get; set; } = 100;
        public string? ControlSequence { get; set; }
        public string Status { get; set; } = "DRAFT";
    }

    /// <summary>
    /// Request for NDT Program approval
    /// </summary>
    public class ApproveNDTProgramRequest
    {
        public bool Approve { get; set; } = true;
        public string? Comments { get; set; }
    }

    /// <summary>
    /// Request to upload NDT Program file
    /// </summary>
    public class NDTProgramFileUploadRequest
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string Base64Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// Request for AI NDT Program generation
    /// </summary>
    public class GenerateNDTProgramRequest
    {
        public Guid? AssetId { get; set; }
        public string? CDCReference { get; set; }
        public string? WeldingProcess { get; set; }
        public string? WeldClass { get; set; }
        public string? MaterialTypes { get; set; }
        public string? ThicknessRange { get; set; }
        public string? AdditionalRequirements { get; set; }
    }

    /// <summary>
    /// Result of AI NDT Program generation
    /// </summary>
    public class GenerateNDTProgramResultDto
    {
        public bool Success { get; set; }
        public string? GeneratedContent { get; set; }
        public string? RequiredControls { get; set; }
        public string? AcceptanceCriteria { get; set; }
        public string? ControlSequence { get; set; }
        public string? AIModelVersion { get; set; }
        public string? PromptUsed { get; set; }
        public List<string> Warnings { get; set; } = new();
        public Guid? CreatedProgramId { get; set; }
    }

    /// <summary>
    /// Request for AI NDT adaptation based on defects found
    /// </summary>
    public class AdaptNDTProgramRequest
    {
        public Guid ProgramId { get; set; }
        public List<DefectInfo> DefectsFound { get; set; } = new();
        public string? AdditionalContext { get; set; }
    }

    public class DefectInfo
    {
        public string DefectType { get; set; } = string.Empty;
        public string? Location { get; set; }
        public string? Size { get; set; }
        public string? ControlType { get; set; }
    }

    /// <summary>
    /// Result of AI NDT Program adaptation
    /// </summary>
    public class AdaptNDTProgramResultDto
    {
        public bool Success { get; set; }
        public string? RecommendedChanges { get; set; }
        public string? UpdatedRequiredControls { get; set; }
        public string? UpdatedAcceptanceCriteria { get; set; }
        public int? RecommendedSamplingRate { get; set; }
        public List<string> Justifications { get; set; } = new();
        public string? AIModelVersion { get; set; }
    }
}
