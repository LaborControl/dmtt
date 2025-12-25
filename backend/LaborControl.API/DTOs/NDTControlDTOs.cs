namespace LaborControl.API.DTOs
{
    /// <summary>
    /// DTO for NDT control list display
    /// </summary>
    public class NDTControlDto
    {
        public Guid Id { get; set; }
        public Guid WeldId { get; set; }
        public string WeldReference { get; set; } = string.Empty;
        public Guid? NDTProgramId { get; set; }
        public string? NDTProgramReference { get; set; }
        public string ControlType { get; set; } = string.Empty;
        public Guid? ControllerId { get; set; }
        public string? ControllerName { get; set; }
        public int? ControllerLevel { get; set; }
        public DateTime? PlannedDate { get; set; }
        public DateTime? ControlDate { get; set; }
        public string Result { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? AppliedStandard { get; set; }
        public bool HasNonConformity { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    /// <summary>
    /// DTO for NDT control detail view
    /// </summary>
    public class NDTControlDetailDto : NDTControlDto
    {
        public string? AcceptanceCriteria { get; set; }
        public string? Comments { get; set; }
        public string? DefectsFound { get; set; }
        public string? ControlParameters { get; set; }
        public string? Photos { get; set; }
        public string? ReportFilePath { get; set; }
        public string? EnvironmentalConditions { get; set; }
        public string? EquipmentUsed { get; set; }
        public string? EquipmentCalibrationNumber { get; set; }
        public DateTime? FirstScanAt { get; set; }
        public DateTime? SecondScanAt { get; set; }
        public Guid? NonConformityId { get; set; }
        public NonConformityDto? NonConformity { get; set; }
        public string? ControllerSignature { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>
    /// Request to create a new NDT control
    /// </summary>
    public class CreateNDTControlRequest
    {
        public Guid WeldId { get; set; }
        public Guid? NDTProgramId { get; set; }
        public string ControlType { get; set; } = "VT";
        public Guid? ControllerId { get; set; }
        public DateTime? PlannedDate { get; set; }
        public string? AppliedStandard { get; set; }
        public string? AcceptanceCriteria { get; set; }
    }

    /// <summary>
    /// Request to update/complete an NDT control
    /// </summary>
    public class UpdateNDTControlRequest
    {
        public Guid? ControllerId { get; set; }
        public DateTime? PlannedDate { get; set; }
        public string? AppliedStandard { get; set; }
        public string? AcceptanceCriteria { get; set; }
        public string Status { get; set; } = "PLANNED";
    }

    /// <summary>
    /// Request for NDT control execution (double scan)
    /// </summary>
    public class NDTControlExecutionRequest
    {
        public bool IsFirstScan { get; set; } = true;
        public string Result { get; set; } = "PENDING";
        public string? Comments { get; set; }
        public string? DefectsFound { get; set; }
        public string? ControlParameters { get; set; }
        public string? Photos { get; set; }
        public string? EnvironmentalConditions { get; set; }
        public string? EquipmentUsed { get; set; }
        public string? EquipmentCalibrationNumber { get; set; }
        public string? ControllerSignature { get; set; }
        public bool CreateNonConformity { get; set; } = false;
        public string? NonConformityDescription { get; set; }
    }

    /// <summary>
    /// Request to upload NDT control report
    /// </summary>
    public class NDTReportUploadRequest
    {
        public string FileName { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public string Base64Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// Dashboard statistics for NDT controls
    /// </summary>
    public class NDTDashboardDto
    {
        public int TotalControls { get; set; }
        public int PlannedControls { get; set; }
        public int InProgressControls { get; set; }
        public int CompletedControls { get; set; }
        public int ConformControls { get; set; }
        public int NonConformControls { get; set; }
        public decimal ConformityRate { get; set; }
        public List<NDTTypeCount> TypeBreakdown { get; set; } = new();
        public List<NDTControllerStats> ControllerStats { get; set; } = new();
    }

    public class NDTTypeCount
    {
        public string ControlType { get; set; } = string.Empty;
        public int Total { get; set; }
        public int Conform { get; set; }
        public int NonConform { get; set; }
    }

    public class NDTControllerStats
    {
        public Guid ControllerId { get; set; }
        public string ControllerName { get; set; } = string.Empty;
        public int ControlsPerformed { get; set; }
        public decimal ConformityRate { get; set; }
    }
}
