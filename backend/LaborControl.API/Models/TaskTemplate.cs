using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    public class TaskTemplate
    {
        public Guid Id { get; set; }

        [Required]
        public Guid CustomerId { get; set; }
        public Customer? Customer { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required]
        [MaxLength(50)]
        public string Category { get; set; } = "SURVEILLANCE";

        public bool IsUniversal { get; set; } = false;
        public bool AlertOnMismatch { get; set; } = true;

        [MaxLength(500)]
        public string? LegalWarning { get; set; }

        public string FormTemplate { get; set; } = "{}";

        /// <summary>
        /// Nécessite un double scan NFC (ouverture + validation) pour renforcer la sécurité
        /// </summary>
        public bool RequireDoubleScan { get; set; } = false;

        /// <summary>
        /// Indique si c'est un protocole prédéfini par le système (true) ou créé par le client (false)
        /// </summary>
        public bool IsPredefined { get; set; } = false;

        /// <summary>
        /// ID du métier (Industry) auquel ce protocole est lié (optionnel, uniquement pour les protocoles prédéfinis)
        /// </summary>
        public Guid? IndustryId { get; set; }
        public Industry? Industry { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public ICollection<ScheduledTask> ScheduledTasks { get; set; } = new List<ScheduledTask>();

        // Nouvelle relation many-to-many avec les qualifications requises
        public ICollection<TaskTemplateQualification> TaskTemplateQualifications { get; set; } = new List<TaskTemplateQualification>();
    }
}
