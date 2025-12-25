using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    public class EquipmentCategory
    {
        public Guid Id { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // "Production", "Utilités", "Sécurité"

        [MaxLength(50)]
        public string? Code { get; set; } // "PROD", "UTIL", "SECU"

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(20)]
        public string? Color { get; set; } // "#3B82F6"

        [MaxLength(10)]
        public string? Icon { get; set; } // Emoji ou code icon

        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public bool IsPredefined { get; set; } = false; // Catégories système vs personnalisées

        public Guid CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public ICollection<EquipmentType> EquipmentTypes { get; set; } = new List<EquipmentType>();
    }
}
