namespace LaborControl.API.Models
{
    public class ScheduledTask
    {
        public Guid Id { get; set; }

        // Type de tâche : PROTOCOL ou MAINTENANCE
        public string TaskType { get; set; } = "PROTOCOL"; // PROTOCOL, MAINTENANCE

        // Pour les PROTOCOLES (existant)
        public Guid? ControlPointId { get; set; }
        public ControlPoint? ControlPoint { get; set; }

        // Pour les MAINTENANCES (nouveau)
        public Guid? AssetId { get; set; }
        public Asset? Asset { get; set; }
        public Guid? MaintenanceScheduleId { get; set; }
        public MaintenanceSchedule? MaintenanceSchedule { get; set; }

        public Guid UserId { get; set; }
        public User? User { get; set; }
        public Guid CustomerId { get; set; }
        public Customer? Customer { get; set; }
        
        // Planning
        public DateTime ScheduledDate { get; set; }
        public DateTime? ScheduledEndDate { get; set; } // Date de fin pour tâches multi-jours
        public TimeSpan? ScheduledTimeStart { get; set; }
        public TimeSpan? ScheduledTimeEnd { get; set; }
        
        // Récurrence
        public string Recurrence { get; set; } = "ONCE"; // ONCE, DAILY, WEEKLY, MONTHLY
        public string WeekendHandling { get; set; } = "ALLOW"; // ALLOW, MOVE_TO_MONDAY, SKIP

        // Sécurisation Double Bornage
        public bool RequireDoubleScan { get; set; } = false; // Nécessite 2 scans NFC (ouverture + validation)

        // Statut
        public string Status { get; set; } = "PENDING"; // PENDING, COMPLETED, OVERDUE, CANCELLED
        public string? CancellationReason { get; set; } // Motif d'annulation (obligatoire si Status = CANCELLED)
        public DateTime? CancelledAt { get; set; } // Date et heure d'annulation
        public Guid? CancelledBy { get; set; } // Utilisateur qui a annulé la tâche

        // Lien avec l'exécution réelle (selon le type)
        public Guid? TaskExecutionId { get; set; }
        public TaskExecution? TaskExecution { get; set; }

        // Pour les MAINTENANCES
        public Guid? MaintenanceExecutionId { get; set; }
        public MaintenanceExecution? MaintenanceExecution { get; set; }
        
        // NOUVELLE PROPRIÉTÉ - Template de tâche flexible
        public Guid? TaskTemplateId { get; set; }
        public TaskTemplate? TaskTemplate { get; set; }

        // Tolérance de retard configurable
        public string DelayToleranceUnit { get; set; } = "MINUTES"; // MINUTES, HOURS, DAYS
        public int DelayToleranceValue { get; set; } = 0; // Nombre d'unités de tolérance (0 = aucune tolérance)

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}