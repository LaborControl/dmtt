namespace LaborControl.API.Models
{
    public class RfidChip
    {
        public Guid Id { get; set; }
        public string ChipId { get; set; } = string.Empty;
        public string Uid { get; set; } = string.Empty;

        // Customer qui possède cette puce (nullable pour les puces en stock)
        public Guid? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        // Commande d'origine de cette puce
        public Guid? OrderId { get; set; }
        public Order? Order { get; set; }

        // Code packaging unique pour la réception client (ex: PKG-2025-001-0001)
        public string? PackagingCode { get; set; }

        // Sécurité anti-clonage (nullable - générés uniquement lors de l'encodage physique)
        public string? Salt { get; set; }  // UUID unique par puce
        public string? Checksum { get; set; }  // HMAC-SHA256(UID + Salt + ChipId)

        public string Status { get; set; } = "IN_STOCK"; // IN_STOCK, IN_TRANSIT, RECEIVED, ASSIGNED, ACTIVE, INACTIVE
        public DateTime ActivationDate { get; set; } = DateTime.UtcNow;
        public DateTime? DeactivationDate { get; set; }
        public DateTime? ReceivedDate { get; set; }

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
        public ControlPoint? ControlPoint { get; set; }
        public DateTime? AssignmentDate { get; set; }
        public DateTime? FirstScanDate { get; set; }
        public DateTime? LastScanDate { get; set; }

        // SAV
        public string? SavReason { get; set; }
        public DateTime? SavReturnDate { get; set; }
        public Guid? ReplacementChipId { get; set; }
        public RfidChip? ReplacementChip { get; set; }

        // Historique des changements de statut
        public List<RfidChipStatusHistory> StatusHistory { get; set; } = new();

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public string? CreatedBy { get; set; }
    }
}
