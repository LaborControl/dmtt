using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    public class Site
    {
        public Guid Id { get; set; }
        
        [Required]
        public Guid CustomerId { get; set; }
        public Customer? Customer { get; set; }
        
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // "Usine Toulouse", "EHPAD Nord"
        
        [MaxLength(50)]
        public string? Code { get; set; } // "SITE-001", "LYN-01"
        
        [MaxLength(255)]
        public string? Address { get; set; }
        
        [MaxLength(100)]
        public string? City { get; set; }
        
        [MaxLength(10)]
        public string? PostalCode { get; set; }
        
        [MaxLength(50)]
        public string? Country { get; set; } = "France";
        
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        
        [MaxLength(50)]
        public string? ContactName { get; set; }
        
        [MaxLength(20)]
        public string? ContactPhone { get; set; }
        
        [MaxLength(100)]
        public string? ContactEmail { get; set; }

        [MaxLength(14)]
        public string? Siret { get; set; } // SIRET de l'établissement (optionnel)

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        
        // Relations
        public ICollection<Zone> Zones { get; set; } = new List<Zone>();
        public ICollection<User> Users { get; set; } = new List<User>();
    }
}