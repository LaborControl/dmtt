using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    /// <summary>
    /// Matériau - Gestion des matériaux avec validation CCPU
    /// </summary>
    public class Material
    {
        public Guid Id { get; set; }

        [Required]
        public Guid CustomerId { get; set; }
        public Customer? Customer { get; set; }

        /// <summary>
        /// Référence matériau
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Reference { get; set; } = string.Empty;

        /// <summary>
        /// Nom du matériau
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Nuance (Grade)
        /// </summary>
        [MaxLength(100)]
        public string? Grade { get; set; }

        /// <summary>
        /// Spécification (ex: EN 10216-2, ASME SA-312)
        /// </summary>
        [MaxLength(100)]
        public string? Specification { get; set; }

        /// <summary>
        /// Numéro de coulée (Heat Number)
        /// </summary>
        [MaxLength(100)]
        public string? HeatNumber { get; set; }

        /// <summary>
        /// Numéro de lot
        /// </summary>
        [MaxLength(100)]
        public string? BatchNumber { get; set; }

        /// <summary>
        /// Fournisseur
        /// </summary>
        [MaxLength(200)]
        public string? Supplier { get; set; }

        /// <summary>
        /// Numéro de certificat matière
        /// </summary>
        [MaxLength(100)]
        public string? CertificateNumber { get; set; }

        /// <summary>
        /// Chemin vers le certificat (Azure Blob)
        /// </summary>
        [MaxLength(500)]
        public string? CertificateFilePath { get; set; }

        /// <summary>
        /// Date de réception
        /// </summary>
        public DateTime? ReceiptDate { get; set; }

        /// <summary>
        /// Quantité reçue
        /// </summary>
        public decimal? Quantity { get; set; }

        /// <summary>
        /// Unité de mesure
        /// </summary>
        [MaxLength(20)]
        public string? Unit { get; set; }

        /// <summary>
        /// Dimensions (JSON) - longueur, largeur, épaisseur, diamètre
        /// </summary>
        public string? Dimensions { get; set; }

        /// <summary>
        /// Statut du matériau
        /// PENDING_RECEIPT, RECEIVED, PENDING_CCPU, APPROVED, REJECTED, IN_USE, CONSUMED
        /// </summary>
        [MaxLength(30)]
        public string Status { get; set; } = "PENDING_RECEIPT";

        /// <summary>
        /// Validateur CCPU
        /// </summary>
        public Guid? CCPUValidatorId { get; set; }
        public User? CCPUValidator { get; set; }

        /// <summary>
        /// Date de validation CCPU
        /// </summary>
        public DateTime? CCPUValidationDate { get; set; }

        /// <summary>
        /// Commentaires CCPU
        /// </summary>
        [MaxLength(1000)]
        public string? CCPUComments { get; set; }

        /// <summary>
        /// Matériau bloqué (non conforme au CDC/normes)
        /// </summary>
        public bool IsBlocked { get; set; } = false;

        /// <summary>
        /// Raison du blocage
        /// </summary>
        [MaxLength(500)]
        public string? BlockReason { get; set; }

        /// <summary>
        /// Sous-traitant ayant fourni le matériau
        /// </summary>
        public Guid? SubcontractorId { get; set; }
        public User? Subcontractor { get; set; }

        /// <summary>
        /// Emplacement de stockage
        /// </summary>
        [MaxLength(200)]
        public string? StorageLocation { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Relations
        public ICollection<NonConformity> NonConformities { get; set; } = new List<NonConformity>();
    }
}
