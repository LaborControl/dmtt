using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    /// <summary>
    /// DMOS - Descriptif de Mode Opératoire de Soudage
    /// Document définissant les paramètres et procédures de soudage
    /// </summary>
    public class DMOS
    {
        public Guid Id { get; set; }

        [Required]
        public Guid CustomerId { get; set; }
        public Customer? Customer { get; set; }

        /// <summary>
        /// Référence du DMOS
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Reference { get; set; } = string.Empty;

        /// <summary>
        /// Nom/Titre du DMOS
        /// </summary>
        [Required]
        [MaxLength(300)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Version du document
        /// </summary>
        [MaxLength(20)]
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// Procédé de soudage (TIG, MIG, MAG, SMAW, etc.)
        /// </summary>
        [MaxLength(50)]
        public string WeldingProcess { get; set; } = "TIG";

        /// <summary>
        /// Matériaux de base concernés
        /// </summary>
        [MaxLength(500)]
        public string? BaseMaterials { get; set; }

        /// <summary>
        /// Plage d'épaisseur applicable
        /// </summary>
        [MaxLength(50)]
        public string? ThicknessRange { get; set; }

        /// <summary>
        /// Plage de diamètre applicable
        /// </summary>
        [MaxLength(50)]
        public string? DiameterRange { get; set; }

        /// <summary>
        /// Positions de soudage qualifiées
        /// </summary>
        [MaxLength(100)]
        public string? QualifiedPositions { get; set; }

        /// <summary>
        /// Métal d'apport
        /// </summary>
        [MaxLength(200)]
        public string? FillerMetal { get; set; }

        /// <summary>
        /// Gaz de protection
        /// </summary>
        [MaxLength(100)]
        public string? ShieldingGas { get; set; }

        /// <summary>
        /// Paramètres de soudage (JSON) - intensité, tension, vitesse, préchauffage
        /// </summary>
        public string? WeldingParameters { get; set; }

        /// <summary>
        /// Normes applicables (JSON array)
        /// </summary>
        public string? ApplicableStandards { get; set; }

        /// <summary>
        /// Chemin vers le fichier PDF (Azure Blob)
        /// </summary>
        [MaxLength(500)]
        public string? FilePath { get; set; }

        /// <summary>
        /// Statut du DMOS
        /// DRAFT, PENDING_APPROVAL, APPROVED, SUPERSEDED, CANCELLED
        /// </summary>
        [MaxLength(30)]
        public string Status { get; set; } = "DRAFT";

        /// <summary>
        /// Généré par IA
        /// </summary>
        public bool GeneratedByAI { get; set; } = false;

        /// <summary>
        /// Version du modèle IA utilisé
        /// </summary>
        [MaxLength(50)]
        public string? AIModelVersion { get; set; }

        /// <summary>
        /// Prompt utilisé pour la génération IA
        /// </summary>
        public string? AIPrompt { get; set; }

        /// <summary>
        /// Approuvé par (Coordinateur soudage / RQ)
        /// </summary>
        public Guid? ApprovedById { get; set; }
        public User? ApprovedBy { get; set; }

        /// <summary>
        /// Date d'approbation
        /// </summary>
        public DateTime? ApprovalDate { get; set; }

        /// <summary>
        /// Commentaires d'approbation
        /// </summary>
        [MaxLength(1000)]
        public string? ApprovalComments { get; set; }

        /// <summary>
        /// Date de validité
        /// </summary>
        public DateTime? ValidUntil { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Relations
        public ICollection<Weld> Welds { get; set; } = new List<Weld>();
    }
}
