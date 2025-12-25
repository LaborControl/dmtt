using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    /// <summary>
    /// Soudure - Entité centrale pour le suivi des opérations de soudage nucléaire
    /// </summary>
    public class Weld
    {
        public Guid Id { get; set; }

        [Required]
        public Guid CustomerId { get; set; }
        public Customer? Customer { get; set; }

        /// <summary>
        /// Repère soudure (ex: "S-001-A", "W-102-B")
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Reference { get; set; } = string.Empty;

        /// <summary>
        /// Équipement/Élément auquel la soudure appartient
        /// </summary>
        [Required]
        public Guid AssetId { get; set; }
        public Asset? Asset { get; set; }

        /// <summary>
        /// Diamètre nominal (DN50, DN100, DN200, etc.)
        /// </summary>
        [MaxLength(20)]
        public string? Diameter { get; set; }

        /// <summary>
        /// Épaisseur en mm
        /// </summary>
        [MaxLength(20)]
        public string? Thickness { get; set; }

        /// <summary>
        /// Matériau tube 1
        /// </summary>
        [MaxLength(100)]
        public string? Material1 { get; set; }

        /// <summary>
        /// Matériau tube 2
        /// </summary>
        [MaxLength(100)]
        public string? Material2 { get; set; }

        /// <summary>
        /// Procédé de soudage (TIG, MIG, MAG, SMAW, FCAW, SAW)
        /// </summary>
        [MaxLength(20)]
        public string WeldingProcess { get; set; } = "TIG";

        /// <summary>
        /// Type de joint (BUTT, LAP, TEE, CORNER, EDGE)
        /// </summary>
        [MaxLength(20)]
        public string JointType { get; set; } = "BUTT";

        /// <summary>
        /// Classe de soudure (A, B, C) selon RCC-M ou normes EDF
        /// </summary>
        [MaxLength(10)]
        public string? WeldClass { get; set; }

        /// <summary>
        /// Position de soudage (1G, 2G, 3G, 4G, 5G, 6G)
        /// </summary>
        [MaxLength(10)]
        public string? WeldingPosition { get; set; }

        /// <summary>
        /// DMOS associé (Descriptif Mode Opératoire de Soudage)
        /// </summary>
        public Guid? DMOSId { get; set; }
        public DMOS? DMOS { get; set; }

        /// <summary>
        /// Soudeur assigné
        /// </summary>
        public Guid? WelderId { get; set; }
        public User? Welder { get; set; }

        /// <summary>
        /// Date d'exécution prévue
        /// </summary>
        public DateTime? PlannedDate { get; set; }

        /// <summary>
        /// Date d'exécution réelle
        /// </summary>
        public DateTime? ExecutionDate { get; set; }

        /// <summary>
        /// Statut de la soudure
        /// PENDING, PLANNED, IN_PROGRESS, WELDED, AWAITING_NDT, NDT_IN_PROGRESS, AWAITING_CCPU, VALIDATED, REJECTED
        /// </summary>
        [MaxLength(30)]
        public string Status { get; set; } = "PENDING";

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
        /// Soudure bloquée (workflow verrouillage)
        /// </summary>
        public bool IsBlocked { get; set; } = false;

        /// <summary>
        /// Raison du blocage
        /// </summary>
        [MaxLength(500)]
        public string? BlockReason { get; set; }

        /// <summary>
        /// Paramètres de soudage (JSON) - température, vitesse, ampérage, etc.
        /// </summary>
        public string? WeldingParameters { get; set; }

        /// <summary>
        /// Photos de la soudure (JSON array d'URLs)
        /// </summary>
        public string? Photos { get; set; }

        /// <summary>
        /// Observations du soudeur
        /// </summary>
        [MaxLength(2000)]
        public string? WelderObservations { get; set; }

        /// <summary>
        /// Premier scan NFC (début soudage)
        /// </summary>
        public DateTime? FirstScanAt { get; set; }

        /// <summary>
        /// Second scan NFC (fin soudage)
        /// </summary>
        public DateTime? SecondScanAt { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Relations
        public ICollection<NDTControl> NDTControls { get; set; } = new List<NDTControl>();
        public ICollection<NonConformity> NonConformities { get; set; } = new List<NonConformity>();
    }
}
