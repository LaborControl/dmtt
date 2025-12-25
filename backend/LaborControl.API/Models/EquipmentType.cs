using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    public class EquipmentType
    {
        public Guid Id { get; set; }

        [Required]
        public Guid EquipmentCategoryId { get; set; }
        public EquipmentCategory? EquipmentCategory { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // "Échangeur", "Pompe", "Compresseur"

        [MaxLength(50)]
        public string? Code { get; set; } // "EXCH", "PUMP", "COMP"

        [MaxLength(500)]
        public string? Description { get; set; }

        [MaxLength(10)]
        public string? Icon { get; set; } // Emoji ou code icon

        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public bool IsPredefined { get; set; } = false; // Types système vs personnalisés

        public Guid CustomerId { get; set; }
        public Customer? Customer { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
