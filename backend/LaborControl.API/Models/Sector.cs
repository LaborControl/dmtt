using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    /// <summary>
    /// Secteur d'activité (ex: Maintenance industrielle, QHSE, Santé, Commerce...)
    /// </summary>
    public class Sector
    {
        public Guid Id { get; set; }

        public Guid CustomerId { get; set; }
        public Customer? Customer { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // "Maintenance industrielle", "QHSE"

        [MaxLength(50)]
        public string? Code { get; set; } // "MAINTENANCE", "QHSE"

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(20)]
        public string? Color { get; set; } // Code couleur hex pour l'UI

        [MaxLength(10)]
        public string? Icon { get; set; } // Emoji ou code icon

        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Indique si c'est un secteur prédéfini par le système (true) ou créé par le client (false)
        /// </summary>
        public bool IsPredefined { get; set; } = false;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public ICollection<Industry> Industries { get; set; } = new List<Industry>();
        public ICollection<Qualification> Qualifications { get; set; } = new List<Qualification>();
    }
}
