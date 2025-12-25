using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    public class ControlPoint
    {
        public Guid Id { get; set; }

        [MaxLength(50)]
        public string Code { get; set; } = string.Empty;

        [MaxLength(200)]
        public string Name { get; set; } = string.Empty;

        public string? Description { get; set; }

        public string? LocationDescription { get; set; }

        // Deprecated - no longer used
        [MaxLength(50)]
        public string? MeasurementType { get; set; }

        public Guid? RfidChipId { get; set; }
        public RfidChip? RfidChip { get; set; }

        /// <summary>
        /// Point de contrôle rattaché à une ZONE (ex: contrôle température couloir EHPAD)
        /// OU à un équipement via AssetId
        /// Au moins l'un des deux doit être renseigné
        /// </summary>
        public Guid? ZoneId { get; set; }
        public Zone? Zone { get; set; }

        /// <summary>
        /// Point de contrôle rattaché à un ÉQUIPEMENT (ex: pression compresseur)
        /// OU à une zone via ZoneId
        /// Au moins l'un des deux doit être renseigné
        /// </summary>
        public Guid? AssetId { get; set; }
        public Asset? Asset { get; set; }

        public Guid CustomerId { get; set; }
        public Customer? Customer { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;
        
        // Navigation properties
        public ICollection<ScheduledTask>? ScheduledTasks { get; set; }
        public ICollection<TaskExecution>? TaskExecutions { get; set; }
    }
}
