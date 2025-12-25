using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    /// <summary>
    /// Métier spécifique dans un secteur d'activité
    /// (ex: "Technicien de maintenance" dans secteur "Maintenance industrielle")
    /// </summary>
    public class Industry
    {
        public Guid Id { get; set; }

        public Guid CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public Guid SectorId { get; set; }
        public Sector? Sector { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // "Technicien de maintenance", "Électricien"

        [MaxLength(50)]
        public string? Code { get; set; } // "TECH_MAINT", "ELECTRICIEN"

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(20)]
        public string? Color { get; set; } // Code couleur hex pour l'UI

        [MaxLength(10)]
        public string? Icon { get; set; } // Emoji ou code icon

        public int DisplayOrder { get; set; } = 0;

        // Qualifications recommandées/requises pour ce métier
        public string? RecommendedQualifications { get; set; } // JSON array de Qualification IDs

        /// <summary>
        /// Indique si c'est un métier prédéfini par le système (true) ou créé par le client (false)
        /// </summary>
        public bool IsPredefined { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
