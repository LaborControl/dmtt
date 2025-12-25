using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    /// <summary>
    /// Historique d'exécution d'une maintenance
    /// Équivalent de TaskExecution mais pour la maintenance
    /// </summary>
    public class MaintenanceExecution
    {
        public Guid Id { get; set; }

        [Required]
        public Guid MaintenanceScheduleId { get; set; }
        public MaintenanceSchedule? MaintenanceSchedule { get; set; }

        /// <summary>
        /// Tâche planifiée associée (liaison avec le planning commun)
        /// </summary>
        public Guid? ScheduledTaskId { get; set; }
        public ScheduledTask? ScheduledTask { get; set; }

        [Required]
        public Guid AssetId { get; set; }
        public Asset? Asset { get; set; }

        [Required]
        public Guid UserId { get; set; }
        public User? User { get; set; }

        [Required]
        public Guid CustomerId { get; set; }

        /// <summary>
        /// Équipe qui a effectué la maintenance
        /// </summary>
        public Guid? TeamId { get; set; }
        public Team? Team { get; set; }

        /// <summary>
        /// Type de maintenance effectuée
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string MaintenanceType { get; set; } = "PREVENTIVE"; // PREVENTIVE, CORRECTIVE, EMERGENCY

        /// <summary>
        /// Statut de l'exécution : STARTED, IN_PROGRESS, COMPLETED, ABORTED
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "STARTED";

        // Timestamps
        public DateTime StartedAt { get; set; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        // Double Bornage - Compatible avec le système existant
        public DateTime? FirstScanAt { get; set; }
        public DateTime? SecondScanAt { get; set; }

        /// <summary>
        /// Durée réelle de la maintenance (calculée)
        /// </summary>
        public TimeSpan? ActualDuration { get; set; }

        /// <summary>
        /// Résultats des tâches de maintenance (JSON)
        /// Ex: [{"taskId": "xxx", "status": "COMPLETED", "measurement": 15.5, "photo": "base64..."}]
        /// </summary>
        public string TaskResults { get; set; } = "{}";

        /// <summary>
        /// Observations générales du technicien
        /// </summary>
        [MaxLength(2000)]
        public string? GeneralObservations { get; set; }

        /// <summary>
        /// Problèmes détectés pendant la maintenance
        /// </summary>
        [MaxLength(2000)]
        public string? IssuesFound { get; set; }

        /// <summary>
        /// Actions correctives effectuées
        /// </summary>
        [MaxLength(2000)]
        public string? CorrectiveActions { get; set; }

        /// <summary>
        /// Recommandations pour les prochaines maintenances
        /// </summary>
        [MaxLength(1000)]
        public string? Recommendations { get; set; }

        /// <summary>
        /// Pièces remplacées (JSON)
        /// Ex: [{"name": "Filtre à air", "reference": "F-123", "oldValue": "usé", "newValue": "neuf"}]
        /// </summary>
        public string? ReplacedParts { get; set; }

        /// <summary>
        /// Consommables utilisés (JSON)
        /// </summary>
        public string? ConsumablesUsed { get; set; }

        /// <summary>
        /// Photos de la maintenance (JSON array d'URLs ou base64)
        /// </summary>
        public string? Photos { get; set; }

        /// <summary>
        /// Signature numérique du technicien (base64)
        /// </summary>
        public string? TechnicianSignature { get; set; }

        /// <summary>
        /// Signature du client/responsable site (base64)
        /// </summary>
        public string? ClientSignature { get; set; }

        /// <summary>
        /// Prochaine maintenance recommandée
        /// </summary>
        public DateTime? NextMaintenanceRecommended { get; set; }

        /// <summary>
        /// Priorité de la prochaine maintenance
        /// </summary>
        [MaxLength(20)]
        public string? NextMaintenancePriority { get; set; }

        /// <summary>
        /// État de l'équipement après maintenance : EXCELLENT, GOOD, FAIR, POOR, CRITICAL
        /// </summary>
        [MaxLength(20)]
        public string? EquipmentCondition { get; set; }

        /// <summary>
        /// Pourcentage d'usure estimé (0-100)
        /// </summary>
        public int? WearPercentage { get; set; }

        /// <summary>
        /// Validation qualité (si requise)
        /// </summary>
        public bool QualityValidated { get; set; } = false;

        /// <summary>
        /// ID de l'utilisateur qui a validé la qualité
        /// </summary>
        public Guid? QualityValidatedBy { get; set; }

        /// <summary>
        /// Date de validation qualité
        /// </summary>
        public DateTime? QualityValidatedAt { get; set; }

        // Flags anti-fraude (comme TaskExecution)
        public bool FlagSaisieRapide { get; set; } = false;
        public bool FlagSaisieDifferee { get; set; } = false;
        public bool FlagValeurRepetee { get; set; } = false;
        public bool FlagHorsMarge { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}