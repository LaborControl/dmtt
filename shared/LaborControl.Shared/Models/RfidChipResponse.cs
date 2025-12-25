namespace LaborControl.Shared.Models;

public class RfidChipResponse
{
    public Guid Id { get; set; }
    public string ChipId { get; set; } = string.Empty;
    public string Uid { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime ActivationDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? PackagingCode { get; set; }

    // Commande fournisseur
    public Guid? SupplierOrderId { get; set; }
    public DateTime? ReceivedFromSupplierDate { get; set; }
    public DateTime? EncodingDate { get; set; }

    // Commande client
    public Guid? ClientOrderId { get; set; }
    public DateTime? ShippedToClientDate { get; set; }
    public DateTime? DeliveredToClientDate { get; set; }

    // Affectation et activation
    public Guid? ControlPointId { get; set; }
    public DateTime? AssignmentDate { get; set; }
    public DateTime? FirstScanDate { get; set; }
    public DateTime? LastScanDate { get; set; }

    // SAV
    public string? SavReason { get; set; }
    public DateTime? SavReturnDate { get; set; }
    public Guid? ReplacementChipId { get; set; }

    // Champs de sécurité cryptographique
    public string? EncryptionKey { get; set; }        // Clef 32 bytes (64 char hex)
    public string? KeyHash { get; set; }              // SHA256 de la clef
    public DateTime? KeyCreatedAt { get; set; }       // Date création clef
    public Guid? KeyCreatedBy { get; set; }           // Utilisateur créateur
    public bool IsKeyProgrammed { get; set; }         // Validation programmation
}
