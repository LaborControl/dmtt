namespace LaborControl.Shared.Models;

/// <summary>
/// Historique des changements de statut d'une puce RFID
/// </summary>
public class RfidChipStatusHistory
{
    public Guid Id { get; set; }
    public Guid RfidChipId { get; set; }
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
    public Guid ChangedBy { get; set; }
    public string? Notes { get; set; }
}
