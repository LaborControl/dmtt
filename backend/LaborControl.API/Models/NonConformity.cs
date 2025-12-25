using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    /// <summary>
    /// NonConformity - Fiche de Non-Conformité (FNC)
    /// Gestion des écarts qualité avec workflow de traitement
    /// </summary>
    public class NonConformity
    {
        public Guid Id { get; set; }

        [Required]
        public Guid CustomerId { get; set; }
        public Customer? Customer { get; set; }

        /// <summary>
        /// Référence FNC (auto-générée)
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Reference { get; set; } = string.Empty;

        /// <summary>
        /// Titre/Objet de la FNC
        /// </summary>
        [Required]
        [MaxLength(300)]
        public string Title { get; set; } = string.Empty;

        /// <summary>
        /// Type de non-conformité
        /// WELD_DEFECT, MATERIAL_DEFECT, DOCUMENTATION, PROCEDURE, QUALIFICATION, OTHER
        /// </summary>
        [MaxLength(30)]
        public string Type { get; set; } = "WELD_DEFECT";

        /// <summary>
        /// Sévérité
        /// MINOR, MAJOR, CRITICAL
        /// </summary>
        [MaxLength(20)]
        public string Severity { get; set; } = "MINOR";

        /// <summary>
        /// Soudure concernée (si applicable)
        /// </summary>
        public Guid? WeldId { get; set; }
        public Weld? Weld { get; set; }

        /// <summary>
        /// Contrôle CND ayant détecté la NC (si applicable)
        /// </summary>
        public Guid? NDTControlId { get; set; }
        public NDTControl? NDTControl { get; set; }

        /// <summary>
        /// Matériau concerné (si applicable)
        /// </summary>
        public Guid? MaterialId { get; set; }
        public Material? Material { get; set; }

        /// <summary>
        /// Équipement/Élément concerné
        /// </summary>
        public Guid? AssetId { get; set; }
        public Asset? Asset { get; set; }

        /// <summary>
        /// Description détaillée de la non-conformité
        /// </summary>
        [Required]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// Cause identifiée
        /// </summary>
        public string? RootCause { get; set; }

        /// <summary>
        /// Statut de la FNC
        /// OPEN, ANALYSIS, PENDING_ACTION, ACTION_IN_PROGRESS, PENDING_VERIFICATION, CLOSED, CANCELLED
        /// </summary>
        [MaxLength(30)]
        public string Status { get; set; } = "OPEN";

        /// <summary>
        /// Créée par
        /// </summary>
        [Required]
        public Guid CreatedById { get; set; }
        public User? CreatedBy { get; set; }

        /// <summary>
        /// Date de détection
        /// </summary>
        public DateTime DetectionDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Action corrective proposée
        /// </summary>
        public string? CorrectiveAction { get; set; }

        /// <summary>
        /// Action préventive (pour éviter récurrence)
        /// </summary>
        public string? PreventiveAction { get; set; }

        /// <summary>
        /// Responsable de l'action corrective
        /// </summary>
        public Guid? ActionResponsibleId { get; set; }
        public User? ActionResponsible { get; set; }

        /// <summary>
        /// Date limite de traitement
        /// </summary>
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// Date de résolution effective
        /// </summary>
        public DateTime? ResolutionDate { get; set; }

        /// <summary>
        /// Clôturée par (RQ)
        /// </summary>
        public Guid? ClosedById { get; set; }
        public User? ClosedBy { get; set; }

        /// <summary>
        /// Date de clôture
        /// </summary>
        public DateTime? ClosedDate { get; set; }

        /// <summary>
        /// Commentaires de clôture
        /// </summary>
        [MaxLength(2000)]
        public string? ClosureComments { get; set; }

        /// <summary>
        /// Photos/Documents associés (JSON array d'URLs)
        /// </summary>
        public string? Attachments { get; set; }

        /// <summary>
        /// Historique des actions (JSON array)
        /// </summary>
        public string? ActionHistory { get; set; }

        /// <summary>
        /// Coût estimé de la non-conformité
        /// </summary>
        public decimal? EstimatedCost { get; set; }

        /// <summary>
        /// Impact sur le planning (jours)
        /// </summary>
        public int? ScheduleImpactDays { get; set; }

        /// <summary>
        /// Nécessite re-contrôle
        /// </summary>
        public bool RequiresRecontrol { get; set; } = false;

        /// <summary>
        /// Contrôle de vérification effectué
        /// </summary>
        public Guid? VerificationControlId { get; set; }

        /// <summary>
        /// Recommandation IA (si générée)
        /// </summary>
        public string? AIRecommendation { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
