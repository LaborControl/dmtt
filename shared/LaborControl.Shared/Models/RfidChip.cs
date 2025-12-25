namespace LaborControl.Shared.Models;

public class RfidChip
{
    public Guid Id { get; set; }
    public string ChipId { get; set; } = string.Empty;
    public string Uid { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? Label { get; set; }
    public string? Checksum { get; set; }
    public DateTime ActivationDate { get; set; }
    public DateTime? DeactivationDate { get; set; }

    // Champs de sécurité cryptographique
    public string? EncryptionKey { get; set; }        // Clef 32 bytes (64 char hex)
    public string? KeyHash { get; set; }              // SHA256 de la clef
    public DateTime? KeyCreatedAt { get; set; }       // Date création clef
    public Guid? KeyCreatedBy { get; set; }           // Utilisateur créateur
    public bool IsKeyProgrammed { get; set; }         // Validation programmation
    public string? PackagingCode { get; set; }        // Code packaging

    public bool IsActive => DeactivationDate == null;
}
