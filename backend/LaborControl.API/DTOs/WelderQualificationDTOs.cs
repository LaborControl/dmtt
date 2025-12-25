namespace LaborControl.API.DTOs
{
    /// <summary>
    /// DTO for welder qualification list display
    /// </summary>
    public class WelderQualificationDto
    {
        public Guid Id { get; set; }
        public Guid? WelderId { get; set; }
        public string? WelderName { get; set; }
        public string QualificationNumber { get; set; } = string.Empty;
        public string? WeldingProcess { get; set; }
        public int? CertificationLevel { get; set; }
        public string? QualifiedMaterials { get; set; }
        public string? ThicknessRange { get; set; }
        public string? DiameterRange { get; set; }
        public string? QualifiedPositions { get; set; }
        public string? QualificationStandard { get; set; }
        public string? CertifyingBody { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsExpired => ExpirationDate < DateTime.UtcNow;
        public bool IsExpiringSoon => ExpirationDate < DateTime.UtcNow.AddDays(30) && !IsExpired;
        public bool AiPreValidated { get; set; }
        public decimal? AiConfidenceScore { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO for welder qualification detail view
    /// </summary>
    public class WelderQualificationDetailDto : WelderQualificationDto
    {
        public DateTime? NextRenewalDate { get; set; }
        public string? CertificateFilePath { get; set; }
        public string? TestCouponReference { get; set; }
        public string? TestCouponPhotoUrl { get; set; }
        public string? QualificationInspector { get; set; }
        public Guid? ValidatedById { get; set; }
        public string? ValidatedByName { get; set; }
        public DateTime? ValidationDate { get; set; }
        public string? ValidationComments { get; set; }
        public string? AIExtractedData { get; set; }
        public string? AIWarnings { get; set; }
        public int WeldsCompleted { get; set; }
        public int WeldsConform { get; set; }
        public int WeldsNonConform { get; set; }
        public int ControlsPerformed { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Request to create a new welder qualification
    /// </summary>
    public class CreateWelderQualificationRequest
    {
        public Guid UserId { get; set; }
        public string QualificationType { get; set; } = "WELDER";
        public string QualificationNumber { get; set; } = string.Empty;
        public string? WeldingProcess { get; set; }
        public int? CertificationLevel { get; set; }
        public string? QualifiedMaterials { get; set; }
        public string? ThicknessRange { get; set; }
        public string? DiameterRange { get; set; }
        public string? QualifiedPositions { get; set; }
        public string? QualificationStandard { get; set; }
        public string? CertifyingBody { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public DateTime? NextRenewalDate { get; set; }
        public string? TestCouponReference { get; set; }
        public string? QualificationInspector { get; set; }
    }

    /// <summary>
    /// Request to update a welder qualification
    /// </summary>
    public class UpdateWelderQualificationRequest
    {
        public string? WeldingProcess { get; set; }
        public int? CertificationLevel { get; set; }
        public string? QualifiedMaterials { get; set; }
        public string? ThicknessRange { get; set; }
        public string? DiameterRange { get; set; }
        public string? QualifiedPositions { get; set; }
        public string? QualificationStandard { get; set; }
        public string? CertifyingBody { get; set; }
        public DateTime ExpirationDate { get; set; }
        public DateTime? NextRenewalDate { get; set; }
        public string? TestCouponReference { get; set; }
        public string? QualificationInspector { get; set; }
        public string Status { get; set; } = "PENDING_VALIDATION";
    }

    /// <summary>
    /// Request for qualification validation by welding coordinator
    /// </summary>
    public class ValidateQualificationRequest
    {
        public bool Approve { get; set; } = true;
        public string? Comments { get; set; }
    }

    /// <summary>
    /// Request to upload qualification certificate
    /// </summary>
    public class QualificationCertificateUploadRequest
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string Base64Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO for AI pre-validation result
    /// </summary>
    public class AIPreValidationResultDto
    {
        public bool Success { get; set; }
        public decimal ConfidenceScore { get; set; }
        public ExtractedQualificationDataDto? ExtractedData { get; set; }
        public List<string> Warnings { get; set; } = new();
        public List<string> ValidationIssues { get; set; } = new();
        public string? AIModelVersion { get; set; }
        public string? Error { get; set; }
    }

    /// <summary>
    /// DTO for extracted qualification data from AI
    /// </summary>
    public class ExtractedQualificationDataDto
    {
        public string? QualificationNumber { get; set; }
        public string? HolderName { get; set; }
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

    /// <summary>
    /// Dashboard for qualifications
    /// </summary>
    public class QualificationDashboardDto
    {
        public int TotalQualifications { get; set; }
        public int ValidQualifications { get; set; }
        public int ExpiringWithin30Days { get; set; }
        public int ExpiredQualifications { get; set; }
        public int AIPreValidatedCount { get; set; }
        public decimal AverageAIConfidence { get; set; }
    }

    public class QualificationTypeCount
    {
        public string Type { get; set; } = string.Empty;
        public int Count { get; set; }
        public int Valid { get; set; }
        public int Expired { get; set; }
    }

    public class UserQualificationSummary
    {
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public int TotalQualifications { get; set; }
        public int ValidQualifications { get; set; }
        public bool HasExpiredQualifications { get; set; }
        public decimal AverageConformityRate { get; set; }
    }
}
