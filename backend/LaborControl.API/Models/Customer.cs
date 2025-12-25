using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    public class Customer
    {
        public Guid Id { get; set; }
        
        [Required]
        [MaxLength(255)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string SubscriptionPlan { get; set; } = "free";

        [MaxLength(100)]
        public string? ContactName { get; set; }

        [MaxLength(255)]
        public string? ContactEmail { get; set; }

        [MaxLength(50)]
        public string? ContactPhone { get; set; }

        [MaxLength(500)]
        public string? Address { get; set; }

        [MaxLength(50)]
        public string? Siret { get; set; }

        [MaxLength(10)]
        public string? ApeCode { get; set; }

        [MaxLength(255)]
        public string? Website { get; set; }

        public string? Description { get; set; }

        /// <summary>
        /// Indique si le client gère plusieurs sites
        /// </summary>
        public bool IsMultiSite { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Relations
        public ICollection<Site> Sites { get; set; } = new List<Site>();
        public ICollection<User> Users { get; set; } = new List<User>();
        public ICollection<Team> Teams { get; set; } = new List<Team>();
        public ICollection<RfidChip> RfidChips { get; set; } = new List<RfidChip>();
        public ICollection<TaskTemplate> TaskTemplates { get; set; } = new List<TaskTemplate>();
    }
}
