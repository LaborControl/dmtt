using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    /// <summary>
    /// Gamme de maintenance préventive pour un équipement
    /// Définit la planification et les tâches de maintenance récurrentes
    /// </summary>
    public class MaintenanceSchedule
    {
        public Guid Id { get; set; }

        [Required]
        public Guid AssetId { get; set; }
        public Asset? Asset { get; set; }

        [Required]
        public Guid CustomerId { get; set; }

        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty; // "Maintenance préventive Compresseur C-01"

        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Type de maintenance : PREVENTIVE, CORRECTIVE, CONDITIONNELLE
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Type { get; set; } = "PREVENTIVE";

        /// <summary>
        /// Priorité : LOW, NORMAL, HIGH, CRITICAL
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Priority { get; set; } = "NORMAL";

        /// <summary>
        /// Fréquence de récurrence : DAILY, WEEKLY, MONTHLY, QUARTERLY, YEARLY, HOURS_BASED
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Frequency { get; set; } = "MONTHLY";

        /// <summary>
        /// Intervalle selon la fréquence (ex: tous les 2 mois = Frequency=MONTHLY, Interval=2)
        /// </summary>
        public int Interval { get; set; } = 1;

        /// <summary>
        /// Pour maintenance basée sur les heures de fonctionnement (en heures)
        /// </summary>
        public int? OperatingHoursInterval { get; set; }

        /// <summary>
        /// Durée estimée de la maintenance (en minutes)
        /// </summary>
        public int EstimatedDurationMinutes { get; set; } = 60;

        /// <summary>
        /// Qualification requise pour effectuer cette maintenance
        /// </summary>
        [MaxLength(50)]
        public string RequiredQualification { get; set; } = "TECH_MAINTENANCE";

        /// <summary>
        /// Équipe assignée par défaut (optionnel)
        /// </summary>
        public Guid? DefaultTeamId { get; set; }
        public Team? DefaultTeam { get; set; }

        /// <summary>
        /// Utilisateur assigné par défaut (optionnel)
        /// </summary>
        public Guid? DefaultUserId { get; set; }
        public User? DefaultUser { get; set; }

        /// <summary>
        /// Prochain entretien planifié
        /// </summary>
        public DateTime? NextMaintenanceDate { get; set; }

        /// <summary>
        /// Dernier entretien effectué
        /// </summary>
        public DateTime? LastMaintenanceDate { get; set; }

        /// <summary>
        /// Instructions spéciales ou avertissements
        /// </summary>
        [MaxLength(1000)]
        public string? SpecialInstructions { get; set; }

        /// <summary>
        /// Pièces de rechange nécessaires (JSON)
        /// Ex: [{"name": "Filtre à air", "reference": "F-123", "quantity": 2}]
        /// </summary>
        public string? SpareParts { get; set; }

        /// <summary>
        /// Outils nécessaires (JSON)
        /// Ex: ["Clé à molette 19mm", "Tournevis cruciforme", "Multimètre"]
        /// </summary>
        public string? RequiredTools { get; set; }

        /// <summary>
        /// Statut de la gamme : ACTIVE, INACTIVE, ARCHIVED
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "ACTIVE";

        /// <summary>
        /// Indique si cette gamme a été générée par l'IA
        /// </summary>
        public bool IsAiGenerated { get; set; } = false;

        /// <summary>
        /// Données techniques du constructeur (JSON)
        /// Utilisé par l'IA pour générer les tâches
        /// </summary>
        public string? ManufacturerData { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Relations
        public ICollection<MaintenanceTask> Tasks { get; set; } = new List<MaintenanceTask>();
        public ICollection<MaintenanceExecution> Executions { get; set; } = new List<MaintenanceExecution>();
        public ICollection<MaintenanceScheduleQualification> MaintenanceScheduleQualifications { get; set; } = new List<MaintenanceScheduleQualification>();
    }
}