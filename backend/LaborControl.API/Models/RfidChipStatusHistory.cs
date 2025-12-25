namespace LaborControl.API.Models
{
    public class RfidChipStatusHistory
    {
        public Guid Id { get; set; }
        public Guid RfidChipId { get; set; }
        public RfidChip? RfidChip { get; set; }

        public string FromStatus { get; set; } = string.Empty;
        public string ToStatus { get; set; } = string.Empty;

        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
        public Guid ChangedBy { get; set; }
        public User? ChangedByUser { get; set; }

        public string? Notes { get; set; }
    }
}
