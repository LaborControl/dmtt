using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    /// <summary>
    /// Qualification personnalisable par client (CACES, habilitations, certifications, etc.)
    /// Système flexible pour tous les métiers : maintenance, nettoyage, sécurité, HSE, etc.
    /// </summary>
    public class Qualification
    {
        public Guid Id { get; set; }

        [Required]
        public Guid CustomerId { get; set; }
        public Customer? Customer { get; set; }

        /// <summary>
        /// Nom de la qualification (ex: "CACES R489 Cat 3 - Nacelle", "Habilitation B2V", "SST")
        /// </summary>
        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Catégorie de qualification pour grouper
        /// Ex: "CACES", "HABILITATION_ELECTRIQUE", "CERTIFICATION_HSE", "FORMATION_SECURITE"
        /// </summary>
        [MaxLength(100)]
        public string Category { get; set; } = "AUTRE";

        /// <summary>
        /// Code court optionnel (ex: "R489-3", "B2V", "SST")
        /// </summary>
        [MaxLength(50)]
        public string? Code { get; set; }

        /// <summary>
        /// Description détaillée de la qualification
        /// </summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// Si la qualification nécessite un renouvellement périodique
        /// </summary>
        public bool RequiresRenewal { get; set; } = false;

        /// <summary>
        /// Durée de validité en mois (ex: 60 pour CACES, 24 pour SST)
        /// </summary>
        public int? ValidityPeriodMonths { get; set; }

        /// <summary>
        /// Niveau de criticité (1-5, 5 étant le plus critique)
        /// Permet d'alerter en priorité sur les qualifs critiques expirées
        /// </summary>
        public int CriticalityLevel { get; set; } = 1;

        /// <summary>
        /// Organisme délivrant généralement cette qualification
        /// Ex: "INRS", "APAVE", "Bureau Veritas"
        /// </summary>
        [MaxLength(200)]
        public string? IssuingOrganization { get; set; }

        /// <summary>
        /// Secteur d'activité auquel cette qualification appartient
        /// </summary>
        public Guid? SectorId { get; set; }
        public Sector? Sector { get; set; }

        /// <summary>
        /// Métier auquel cette qualification est associée (plus précis que le secteur)
        /// Exemple: "Habilitation B2V" → Métier "Électricien" → Secteur "Maintenance industrielle"
        /// </summary>
        public Guid? IndustryId { get; set; }
        public Industry? Industry { get; set; }

        /// <summary>
        /// Icône (emoji) pour la qualification
        /// </summary>
        [MaxLength(10)]
        public string? Icon { get; set; }

        /// <summary>
        /// Couleur pour la qualification (format hex #RRGGBB)
        /// </summary>
        [MaxLength(7)]
        public string? Color { get; set; }

        /// <summary>
        /// Ordre d'affichage
        /// </summary>
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Si true, cette qualification fait partie des qualifications prédéfinies
        /// </summary>
        public bool IsPredefined { get; set; } = false;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Relations
        public ICollection<UserQualification> UserQualifications { get; set; } = new List<UserQualification>();
        public ICollection<TaskTemplateQualification> TaskTemplateQualifications { get; set; } = new List<TaskTemplateQualification>();
        public ICollection<MaintenanceScheduleQualification> MaintenanceScheduleQualifications { get; set; } = new List<MaintenanceScheduleQualification>();
    }
}
