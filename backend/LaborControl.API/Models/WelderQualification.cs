using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    /// <summary>
    /// WelderQualification - Qualification spécifique soudeur/contrôleur CND
    /// Extension de UserQualification avec données spécifiques nucléaire
    /// </summary>
    public class WelderQualification
    {
        public Guid Id { get; set; }

        [Required]
        public Guid CustomerId { get; set; }
        public Customer? Customer { get; set; }

        /// <summary>
        /// Utilisateur qualifié
        /// </summary>
        [Required]
        public Guid UserId { get; set; }
        public User? User { get; set; }

        /// <summary>
        /// Type de qualification
        /// WELDER, NDT_VT, NDT_PT, NDT_MT, NDT_RT, NDT_UT, NDT_ET
        /// </summary>
        [Required]
        [MaxLength(30)]
        public string QualificationType { get; set; } = "WELDER";

        /// <summary>
        /// Numéro de qualification/certificat
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string QualificationNumber { get; set; } = string.Empty;

        /// <summary>
        /// Procédé de soudage qualifié (pour soudeurs)
        /// TIG, MIG, MAG, SMAW, FCAW, SAW
        /// </summary>
        [MaxLength(50)]
        public string? WeldingProcess { get; set; }

        /// <summary>
        /// Niveau de certification CND (1, 2, 3 pour contrôleurs)
        /// </summary>
        public int? CertificationLevel { get; set; }

        /// <summary>
        /// Matériaux qualifiés
        /// </summary>
        [MaxLength(500)]
        public string? QualifiedMaterials { get; set; }

        /// <summary>
        /// Plage d'épaisseur qualifiée
        /// </summary>
        [MaxLength(50)]
        public string? ThicknessRange { get; set; }

        /// <summary>
        /// Plage de diamètre qualifiée
        /// </summary>
        [MaxLength(50)]
        public string? DiameterRange { get; set; }

        /// <summary>
        /// Positions qualifiées (1G, 2G, 3G, 4G, 5G, 6G)
        /// </summary>
        [MaxLength(100)]
        public string? QualifiedPositions { get; set; }

        /// <summary>
        /// Norme de qualification (EN ISO 9606, ASME Section IX, etc.)
        /// </summary>
        [MaxLength(100)]
        public string? QualificationStandard { get; set; }

        /// <summary>
        /// Organisme certificateur
        /// </summary>
        [MaxLength(200)]
        public string? CertifyingBody { get; set; }

        /// <summary>
        /// Date d'obtention
        /// </summary>
        public DateTime IssueDate { get; set; }

        /// <summary>
        /// Date d'expiration
        /// </summary>
        public DateTime ExpirationDate { get; set; }

        /// <summary>
        /// Date du prochain renouvellement requis
        /// </summary>
        public DateTime? NextRenewalDate { get; set; }

        /// <summary>
        /// Chemin vers le certificat (Azure Blob)
        /// </summary>
        [MaxLength(500)]
        public string? CertificateFilePath { get; set; }

        /// <summary>
        /// Référence éprouvette de qualification
        /// </summary>
        [MaxLength(200)]
        public string? TestCouponReference { get; set; }

        /// <summary>
        /// Photo éprouvette (Azure Blob)
        /// </summary>
        [MaxLength(500)]
        public string? TestCouponPhotoUrl { get; set; }

        /// <summary>
        /// Inspecteur ayant qualifié
        /// </summary>
        [MaxLength(200)]
        public string? QualificationInspector { get; set; }

        /// <summary>
        /// Statut de la qualification
        /// PENDING_VALIDATION, VALID, EXPIRED, SUSPENDED, REVOKED
        /// </summary>
        [MaxLength(30)]
        public string Status { get; set; } = "PENDING_VALIDATION";

        /// <summary>
        /// Validé par (Coordinateur soudage)
        /// </summary>
        public Guid? ValidatedById { get; set; }
        public User? ValidatedBy { get; set; }

        /// <summary>
        /// Date de validation interne
        /// </summary>
        public DateTime? ValidationDate { get; set; }

        /// <summary>
        /// Commentaires de validation
        /// </summary>
        [MaxLength(1000)]
        public string? ValidationComments { get; set; }

        /// <summary>
        /// Pré-validé par IA (Gemini OCR)
        /// </summary>
        public bool PreValidatedByAI { get; set; } = false;

        /// <summary>
        /// Données extraites par IA (JSON)
        /// </summary>
        public string? AIExtractedData { get; set; }

        /// <summary>
        /// Score de confiance IA (0-1)
        /// </summary>
        public decimal? AIConfidenceScore { get; set; }

        /// <summary>
        /// Alertes IA (JSON array)
        /// </summary>
        public string? AIWarnings { get; set; }

        // Métriques de performance
        /// <summary>
        /// Nombre de soudures réalisées
        /// </summary>
        public int WeldsCompleted { get; set; } = 0;

        /// <summary>
        /// Nombre de soudures conformes
        /// </summary>
        public int WeldsConform { get; set; } = 0;

        /// <summary>
        /// Nombre de soudures non conformes
        /// </summary>
        public int WeldsNonConform { get; set; } = 0;

        /// <summary>
        /// Taux de conformité (calculé)
        /// </summary>
        public decimal ConformityRate => WeldsCompleted > 0
            ? Math.Round((decimal)WeldsConform / WeldsCompleted * 100, 2)
            : 0;

        /// <summary>
        /// Nombre de contrôles effectués (pour CND)
        /// </summary>
        public int ControlsPerformed { get; set; } = 0;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
