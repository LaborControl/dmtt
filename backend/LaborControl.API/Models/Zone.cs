using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    public class Zone
    {
        public Guid Id { get; set; }
        
        [Required]
        public Guid SiteId { get; set; }
        public Site? Site { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // "Bâtiment A", "Zone Production", "Étage 1"
        
        [MaxLength(50)]
        public string? Code { get; set; } // "BAT-A", "PROD-01", "E1"
        
        [MaxLength(50)]
        public string? Type { get; set; } // "BUILDING", "FLOOR", "AREA", "DEPARTMENT"
        
        [MaxLength(500)]
        public string? Description { get; set; }
        
        // Pour créer une hiérarchie de zones (ex: Bâtiment > Étage > Aile)
        public Guid? ParentZoneId { get; set; }
        public Zone? ParentZone { get; set; }
        
        // Niveau dans la hiérarchie (0 = racine, 1 = sous-zone, etc.)
        public int Level { get; set; } = 0;
        
        // Ordre d'affichage
        public int DisplayOrder { get; set; } = 0;
        
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Relations
        public ICollection<Zone> SubZones { get; set; } = new List<Zone>();
        public ICollection<Asset> Assets { get; set; } = new List<Asset>();
        public ICollection<ControlPoint> ControlPoints { get; set; } = new List<ControlPoint>();
    }
}