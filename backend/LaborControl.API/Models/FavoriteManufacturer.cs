using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    /// <summary>
    /// Fabricants favoris par client
    /// Permet de définir une liste de fabricants fréquemment utilisés pour l'auto-complétion
    /// </summary>
    public class FavoriteManufacturer
    {
        public Guid Id { get; set; }

        [Required]
        public Guid CustomerId { get; set; }
        public Customer? Customer { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Pays du fabricant
        /// </summary>
        [MaxLength(100)]
        public string? Country { get; set; }

        /// <summary>
        /// Site web du fabricant
        /// </summary>
        [MaxLength(200)]
        public string? Website { get; set; }

        /// <summary>
        /// Email de contact
        /// </summary>
        [MaxLength(100)]
        public string? ContactEmail { get; set; }

        /// <summary>
        /// Téléphone de contact
        /// </summary>
        [MaxLength(50)]
        public string? ContactPhone { get; set; }

        /// <summary>
        /// Ordre d'affichage
        /// </summary>
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Indique si c'est actif
        /// </summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
