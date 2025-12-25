namespace LaborControl.Shared.Models
{
    public class RfidChipDto
    {
        public Guid Id { get; set; }
        public string ChipId { get; set; } = "";
        public string Uid { get; set; } = "";
        public string Status { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public string? EncryptionKey { get; set; }
        public DateTime? KeyCreatedAt { get; set; }
        public bool IsKeyProgrammed { get; set; }
        public string? PackagingCode { get; set; }
        public Guid? ControlPointId { get; set; }
        public List<StatusChangeDto> StatusHistory { get; set; } = new();
        public string? CustomerName { get; set; }
        public string? SiteName { get; set; }
    }

    public class StatusChangeDto
    {
        public string Status { get; set; } = "";
        public DateTime ChangedAt { get; set; }
        public string? Reason { get; set; }
    }
}
