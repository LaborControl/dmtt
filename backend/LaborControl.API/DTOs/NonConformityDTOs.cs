namespace LaborControl.API.DTOs
{
    /// <summary>
    /// DTO for non-conformity list display
    /// </summary>
    public class NonConformityDto
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string Severity { get; set; } = string.Empty;
        public Guid? WeldId { get; set; }
        public string? WeldReference { get; set; }
        public Guid? MaterialId { get; set; }
        public string? MaterialReference { get; set; }
        public Guid? AssetId { get; set; }
        public string? AssetName { get; set; }
        public string Status { get; set; } = string.Empty;
        public Guid CreatedById { get; set; }
        public string CreatedByName { get; set; } = string.Empty;
        public DateTime DetectionDate { get; set; }
        public DateTime? DueDate { get; set; }
        public Guid? ActionResponsibleId { get; set; }
        public string? ActionResponsibleName { get; set; }
        public bool RequiresRecontrol { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO for non-conformity detail view
    /// </summary>
    public class NonConformityDetailDto : NonConformityDto
    {
        public string Description { get; set; } = string.Empty;
        public string? RootCause { get; set; }
        public string? CorrectiveAction { get; set; }
        public string? PreventiveAction { get; set; }
        public DateTime? ResolutionDate { get; set; }
        public Guid? ClosedById { get; set; }
        public string? ClosedByName { get; set; }
        public DateTime? ClosedDate { get; set; }
        public string? ClosureComments { get; set; }
        public string? Attachments { get; set; }
        public string? ActionHistory { get; set; }
        public decimal? EstimatedCost { get; set; }
        public int? ScheduleImpactDays { get; set; }
        public Guid? VerificationControlId { get; set; }
        public string? AIRecommendation { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public Guid? NDTControlId { get; set; }
        public NDTControlDto? NDTControl { get; set; }
    }

    /// <summary>
    /// Request to create a new non-conformity
    /// </summary>
    public class CreateNonConformityRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = "WELD_DEFECT";
        public string Severity { get; set; } = "MINOR";
        public Guid? WeldId { get; set; }
        public Guid? MaterialId { get; set; }
        public Guid? AssetId { get; set; }
        public string Description { get; set; } = string.Empty;
        public DateTime? DueDate { get; set; }
        public Guid? ActionResponsibleId { get; set; }
        public bool RequiresRecontrol { get; set; } = false;
    }

    /// <summary>
    /// Request to update a non-conformity
    /// </summary>
    public class UpdateNonConformityRequest
    {
        public string Title { get; set; } = string.Empty;
        public string Type { get; set; } = "WELD_DEFECT";
        public string Severity { get; set; } = "MINOR";
        public string Description { get; set; } = string.Empty;
        public string? RootCause { get; set; }
        public string? CorrectiveAction { get; set; }
        public string? PreventiveAction { get; set; }
        public DateTime? DueDate { get; set; }
        public Guid? ActionResponsibleId { get; set; }
        public decimal? EstimatedCost { get; set; }
        public int? ScheduleImpactDays { get; set; }
        public bool RequiresRecontrol { get; set; } = false;
    }

    /// <summary>
    /// Request to change non-conformity status
    /// </summary>
    public class NonConformityStatusChangeRequest
    {
        public string NewStatus { get; set; } = string.Empty;
        public string? Comments { get; set; }
        public string? ActionDetails { get; set; }
    }

    /// <summary>
    /// Request to close a non-conformity
    /// </summary>
    public class CloseNonConformityRequest
    {
        public string? ClosureComments { get; set; }
        public Guid? VerificationControlId { get; set; }
    }

    /// <summary>
    /// Request to add attachment to non-conformity
    /// </summary>
    public class NonConformityAttachmentRequest
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string Base64Content { get; set; } = string.Empty;
        public string? Description { get; set; }
    }

    /// <summary>
    /// Dashboard statistics for non-conformities
    /// </summary>
    public class NonConformityDashboardDto
    {
        public int TotalNonConformities { get; set; }
        public int OpenNonConformities { get; set; }
        public int InProgressNonConformities { get; set; }
        public int ClosedNonConformities { get; set; }
        public int OverdueNonConformities { get; set; }
        public int CriticalNonConformities { get; set; }
        public int MajorNonConformities { get; set; }
        public int MinorNonConformities { get; set; }
        public decimal? TotalEstimatedCost { get; set; }
        public int TotalScheduleImpactDays { get; set; }
        public List<NonConformityTypeCount> TypeBreakdown { get; set; } = new();
        public List<NonConformityStatusCount> StatusBreakdown { get; set; } = new();
    }

    public class NonConformityTypeCount
    {
        public string Type { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class NonConformityStatusCount
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
