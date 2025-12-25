using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    /// <summary>
    /// Table de référence maître des types d'équipement prédéfinis
    /// Ces types sont gérés par l'équipe Labor Control (app-staff)
    /// et peuvent être initialisés par les clients via l'app-client
    /// </summary>
    public class PredefinedEquipmentType
    {
        [Key]
        public Guid Id { get; set; }

        /// <summary>
        /// Référence vers la catégorie d'équipement prédéfinie à laquelle appartient ce type
        /// </summary>
        public Guid PredefinedEquipmentCategoryId { get; set; }
        public PredefinedEquipmentCategory? PredefinedEquipmentCategory { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Code { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Icône emoji ou nom d'icône
        /// </summary>
        [MaxLength(10)]
        public string? Icon { get; set; }

        /// <summary>
        /// Ordre d'affichage au sein de la catégorie (plus petit = affiché en premier)
        /// </summary>
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Indique si ce type est actif et disponible pour initialisation
        /// </summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
