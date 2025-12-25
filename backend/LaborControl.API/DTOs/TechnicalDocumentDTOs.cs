namespace LaborControl.API.DTOs
{
    /// <summary>
    /// DTO for technical document list display
    /// </summary>
    public class TechnicalDocumentDto
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string Version { get; set; } = string.Empty;
        public string? RevisionIndex { get; set; }
        public string? OriginalFileName { get; set; }
        public string? MimeType { get; set; }
        public long? FileSize { get; set; }
        public Guid? AssetId { get; set; }
        public string? AssetName { get; set; }
        public Guid? WeldId { get; set; }
        public string? WeldReference { get; set; }
        public Guid UploadedById { get; set; }
        public string UploadedByName { get; set; } = string.Empty;
        public DateTime UploadDate { get; set; }
        public DateTime? DocumentDate { get; set; }
        public DateTime? ValidUntil { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsExpired => ValidUntil.HasValue && ValidUntil.Value < DateTime.UtcNow;
        public bool AnalyzedByAI { get; set; }
        public bool IsConfidential { get; set; }
        public int DownloadCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO for technical document detail view
    /// </summary>
    public class TechnicalDocumentDetailDto : TechnicalDocumentDto
    {
        public string? Description { get; set; }
        public string FilePath { get; set; } = string.Empty;
        public Guid? ApprovedById { get; set; }
        public string? ApprovedByName { get; set; }
        public DateTime? ApprovalDate { get; set; }
        public string? Issuer { get; set; }
        public string? Tags { get; set; }
        public string? AIExtractedMetadata { get; set; }
        public string? ExtractedText { get; set; }
        public string? AccessRoles { get; set; }
        public DateTime? LastAccessedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Request to create/upload a technical document
    /// </summary>
    public class CreateTechnicalDocumentRequest
    {
        public string Reference { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "OTHER";
        public string? Category { get; set; }
        public string? Description { get; set; }
        public string Version { get; set; } = "1.0";
        public string? RevisionIndex { get; set; }
        public Guid? AssetId { get; set; }
        public Guid? WeldId { get; set; }
        public DateTime? DocumentDate { get; set; }
        public DateTime? ValidUntil { get; set; }
        public string? Issuer { get; set; }
        public string? Tags { get; set; }
        public bool IsConfidential { get; set; } = false;
        public string? AccessRoles { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string Base64Content { get; set; } = string.Empty;
        public bool AnalyzeWithAI { get; set; } = false;
    }

    /// <summary>
    /// Request to update a technical document metadata
    /// </summary>
    public class UpdateTechnicalDocumentRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "OTHER";
        public string? Category { get; set; }
        public string? Description { get; set; }
        public string? RevisionIndex { get; set; }
        public Guid? AssetId { get; set; }
        public Guid? WeldId { get; set; }
        public DateTime? DocumentDate { get; set; }
        public DateTime? ValidUntil { get; set; }
        public string? Issuer { get; set; }
        public string? Tags { get; set; }
        public bool IsConfidential { get; set; } = false;
        public string? AccessRoles { get; set; }
        public string Status { get; set; } = "DRAFT";
    }

    /// <summary>
    /// Request for document approval
    /// </summary>
    public class ApproveDocumentRequest
    {
        public bool Approve { get; set; } = true;
        public string? Comments { get; set; }
    }

    /// <summary>
    /// Request for AI document analysis
    /// </summary>
    public class AnalyzeDocumentRequest
    {
        public Guid DocumentId { get; set; }
        public bool ExtractText { get; set; } = true;
        public bool ExtractMetadata { get; set; } = true;
        public string? AdditionalInstructions { get; set; }
    }

    /// <summary>
    /// Result of AI document analysis
    /// </summary>
    public class AnalyzeDocumentResultDto
    {
        public bool Success { get; set; }
        public string? ExtractedText { get; set; }
        public string? ExtractedMetadata { get; set; }
        public string? DocumentSummary { get; set; }
        public List<string> DetectedKeywords { get; set; } = new();
        public string? AIModelVersion { get; set; }
        public List<string> Warnings { get; set; } = new();
    }

    /// <summary>
    /// Request for document search
    /// </summary>
    public class SearchDocumentsRequest
    {
        public string? Query { get; set; }
        public string? Type { get; set; }
        public string? Category { get; set; }
        public Guid? AssetId { get; set; }
        public Guid? WeldId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public bool? IsExpired { get; set; }
        public bool? AnalyzedByAI { get; set; }
        public string? Tags { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    /// <summary>
    /// Result of document search
    /// </summary>
    public class SearchDocumentsResultDto
    {
        public List<TechnicalDocumentDto> Documents { get; set; } = new();
        public int TotalCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
    }

    /// <summary>
    /// Dashboard for technical documents
    /// </summary>
    public class DocumentDashboardDto
    {
        public int TotalDocuments { get; set; }
        public int ApprovedDocuments { get; set; }
        public int PendingApproval { get; set; }
        public int ExpiredDocuments { get; set; }
        public int ExpiringSoonDocuments { get; set; }
        public int AnalyzedByAI { get; set; }
        public int ConfidentialDocuments { get; set; }
        public long TotalStorageBytes { get; set; }
        public List<DocumentTypeCount> TypeBreakdown { get; set; } = new();
    }

    public class DocumentTypeCount
    {
        public string Type { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
