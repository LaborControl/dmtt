using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    /// <summary>
    /// Table de référence maître des secteurs d'activité prédéfinis
    /// Ces secteurs sont gérés par l'équipe Labor Control (app-staff)
    /// et peuvent être initialisés par les clients via l'app-client
    /// </summary>
    public class PredefinedSector
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Code { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Couleur du secteur (format hex: #RRGGBB)
        /// </summary>
        [MaxLength(20)]
        public string? Color { get; set; }

        /// <summary>
        /// Icône emoji ou nom d'icône
        /// </summary>
        [MaxLength(50)]
        public string? Icon { get; set; }

        /// <summary>
        /// Ordre d'affichage (plus petit = affiché en premier)
        /// </summary>
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Indique si ce secteur est actif et disponible pour initialisation
        /// </summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
