namespace LaborControl.API.DTOs
{
    /// <summary>
    /// DTO for weld list display
    /// </summary>
    public class WeldDto
    {
        public Guid Id { get; set; }
        public string Reference { get; set; } = string.Empty;
        public Guid AssetId { get; set; }
        public string AssetName { get; set; } = string.Empty;
        public string? Diameter { get; set; }
        public string? Thickness { get; set; }
        public string? Material1 { get; set; }
        public string? Material2 { get; set; }
        public string WeldingProcess { get; set; } = string.Empty;
        public string JointType { get; set; } = string.Empty;
        public string? WeldClass { get; set; }
        public string? WeldingPosition { get; set; }
        public Guid? DMOSId { get; set; }
        public string? DMOSReference { get; set; }
        public Guid? WelderId { get; set; }
        public string? WelderName { get; set; }
        public DateTime? PlannedDate { get; set; }
        public DateTime? ExecutionDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public bool IsBlocked { get; set; }
        public bool IsCCPUValidated { get; set; }
        public int NDTControlsCount { get; set; }
        public int NonConformitiesCount { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO for weld detail view
    /// </summary>
    public class WeldDetailDto : WeldDto
    {
        public Guid? CCPUValidatorId { get; set; }
        public string? CCPUValidatorName { get; set; }
        public DateTime? CCPUValidationDate { get; set; }
        public string? CCPUComments { get; set; }
        public string? BlockReason { get; set; }
        public string? WeldingParameters { get; set; }
        public string? Photos { get; set; }
        public string? WelderObservations { get; set; }
        public DateTime? FirstScanAt { get; set; }
        public DateTime? SecondScanAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<NDTControlDto> NDTControls { get; set; } = new();
        public List<NonConformityDto> NonConformities { get; set; } = new();
    }

    /// <summary>
    /// Request to create a new weld
    /// </summary>
    public class CreateWeldRequest
    {
        public string Reference { get; set; } = string.Empty;
        public Guid AssetId { get; set; }
        public string? Diameter { get; set; }
        public string? Thickness { get; set; }
        public string? Material1 { get; set; }
        public string? Material2 { get; set; }
        public string WeldingProcess { get; set; } = "TIG";
        public string JointType { get; set; } = "BW";
        public string? WeldClass { get; set; }
        public string? WeldingPosition { get; set; }
        public Guid? DMOSId { get; set; }
        public Guid? WelderId { get; set; }
        public DateTime? PlannedDate { get; set; }
        public string? WeldingParameters { get; set; }
    }

    /// <summary>
    /// Request to update a weld
    /// </summary>
    public class UpdateWeldRequest
    {
        public Guid AssetId { get; set; }
        public string? Diameter { get; set; }
        public string? Thickness { get; set; }
        public string? Material1 { get; set; }
        public string? Material2 { get; set; }
        public string WeldingProcess { get; set; } = "TIG";
        public string JointType { get; set; } = "BW";
        public string? WeldClass { get; set; }
        public string? WeldingPosition { get; set; }
        public Guid? DMOSId { get; set; }
        public Guid? WelderId { get; set; }
        public DateTime? PlannedDate { get; set; }
        public string? WeldingParameters { get; set; }
        public string? WelderObservations { get; set; }
        public string Status { get; set; } = "PLANNED";
    }

    /// <summary>
    /// Request for CCPU validation
    /// </summary>
    public class CCPUValidationRequest
    {
        public string? Comments { get; set; }
        public bool Approve { get; set; } = true;
        public string? BlockReason { get; set; }
    }

    /// <summary>
    /// Request for weld execution (double scan)
    /// </summary>
    public class WeldExecutionScanRequest
    {
        public bool IsFirstScan { get; set; } = true;
        public string? WelderObservations { get; set; }
        public string? Photos { get; set; }
    }

    /// <summary>
    /// Dashboard statistics for welds
    /// </summary>
    public class WeldDashboardDto
    {
        public int TotalWelds { get; set; }
        public int PlannedWelds { get; set; }
        public int InProgressWelds { get; set; }
        public int CompletedWelds { get; set; }
        public int PendingCCPUValidation { get; set; }
        public int PendingNDTControl { get; set; }
        public int WithNonConformities { get; set; }
        public int BlockedWelds { get; set; }
        public decimal ConformityRate { get; set; }
        public List<WeldStatusCount> StatusBreakdown { get; set; } = new();
    }

    public class WeldStatusCount
    {
        public string Status { get; set; } = string.Empty;
        public int Count { get; set; }
    }
}
