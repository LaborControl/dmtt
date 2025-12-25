using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    /// <summary>
    /// Table de référence maître des métiers prédéfinis
    /// Ces métiers sont gérés par l'équipe Labor Control (app-staff)
    /// et peuvent être initialisés par les clients via l'app-client
    /// </summary>
    public class PredefinedIndustry
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Référence vers le secteur prédéfini auquel appartient ce métier
        /// </summary>
        public Guid PredefinedSectorId { get; set; }
        public PredefinedSector? PredefinedSector { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Code { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Couleur du métier (format hex: #RRGGBB)
        /// </summary>
        [MaxLength(20)]
        public string? Color { get; set; }

        /// <summary>
        /// Icône emoji ou nom d'icône
        /// </summary>
        [MaxLength(50)]
        public string? Icon { get; set; }

        /// <summary>
        /// Ordre d'affichage au sein du secteur (plus petit = affiché en premier)
        /// </summary>
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Qualifications recommandées pour ce métier (format JSON)
        /// </summary>
        public string? RecommendedQualifications { get; set; }

        /// <summary>
        /// Indique si ce métier est actif et disponible pour initialisation
        /// </summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
