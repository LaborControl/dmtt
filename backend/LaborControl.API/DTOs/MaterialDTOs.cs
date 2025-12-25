namespace LaborControl.API.DTOs
{
    /// <summary>
    /// DTO for material list display
    /// </summary>
    public class MaterialDto
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Grade { get; set; }
        public string? Specification { get; set; }
        public string? HeatNumber { get; set; }
        public string? BatchNumber { get; set; }
        public string? Supplier { get; set; }
        public string? CertificateNumber { get; set; }
        public DateTime? ReceiptDate { get; set; }
        public decimal? Quantity { get; set; }
        public string? Unit { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsBlocked { get; set; }
        public bool IsCCPUValidated { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO for material detail view
    /// </summary>
    public class MaterialDetailDto : MaterialDto
    {
        public string? Dimensions { get; set; }
        public string? CertificateFilePath { get; set; }
        public Guid? CCPUValidatorId { get; set; }
        public string? CCPUValidatorName { get; set; }
        public DateTime? CCPUValidationDate { get; set; }
        public string? CCPUComments { get; set; }
        public string? BlockReason { get; set; }
        public Guid? SubcontractorId { get; set; }
        public string? SubcontractorName { get; set; }
        public string? StorageLocation { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<NonConformityDto> NonConformities { get; set; } = new();
    }

    /// <summary>
    /// Request to create a new material
    /// </summary>
    public class CreateMaterialRequest
    {
        public string Reference { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Grade { get; set; }
        public string? Specification { get; set; }
        public string? HeatNumber { get; set; }
        public string? BatchNumber { get; set; }
        public string? Supplier { get; set; }
        public string? CertificateNumber { get; set; }
        public DateTime? ReceiptDate { get; set; }
        public decimal? Quantity { get; set; }
        public string? Unit { get; set; }
        public string? Dimensions { get; set; }
        public Guid? SubcontractorId { get; set; }
        public string? StorageLocation { get; set; }
    }

    /// <summary>
    /// Request to update a material
    /// </summary>
    public class UpdateMaterialRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Grade { get; set; }
        public string? Specification { get; set; }
        public string? HeatNumber { get; set; }
        public string? BatchNumber { get; set; }
        public string? Supplier { get; set; }
        public string? CertificateNumber { get; set; }
        public DateTime? ReceiptDate { get; set; }
        public decimal? Quantity { get; set; }
        public string? Unit { get; set; }
        public string? Dimensions { get; set; }
        public Guid? SubcontractorId { get; set; }
        public string? StorageLocation { get; set; }
        public string Status { get; set; } = "PENDING_VALIDATION";
    }

    /// <summary>
    /// Request for CCPU material validation
    /// </summary>
    public class MaterialCCPUValidationRequest
    {
        public string? Comments { get; set; }
        public bool Approve { get; set; } = true;
        public string? BlockReason { get; set; }
    }

    /// <summary>
    /// Request to upload material certificate
    /// </summary>
    public class MaterialCertificateUploadRequest
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string Base64Content { get; set; } = string.Empty;
    }
}
