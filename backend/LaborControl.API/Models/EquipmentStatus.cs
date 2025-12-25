using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    /// <summary>
    /// Statuts d'équipement personnalisables par client
    /// Permet de définir les statuts possibles pour les équipements (OPERATIONAL, MAINTENANCE, STOPPED, etc.)
    /// </summary>
    public class EquipmentStatus
    {
        public Guid Id { get; set; }

        [Required]
        public Guid CustomerId { get; set; }
        public Customer? Customer { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Code { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Couleur du statut (format hex: #RRGGBB)
        /// </summary>
        [MaxLength(20)]
        public string? Color { get; set; }

        /// <summary>
        /// Icône emoji ou nom d'icône
        /// </summary>
        [MaxLength(10)]
        public string? Icon { get; set; }

        /// <summary>
        /// Ordre d'affichage
        /// </summary>
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Indique si c'est un statut actif
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Indique si c'est un statut prédéfini
        /// </summary>
        public bool IsPredefined { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
