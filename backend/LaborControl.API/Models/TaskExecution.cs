namespace LaborControl.API.Models
{
    public class TaskExecution
    {
        public Guid Id { get; set; }
        
        // Relations
        public Guid? ScheduledTaskId { get; set; }
        public ScheduledTask? ScheduledTask { get; set; }
        
        public Guid ControlPointId { get; set; }
        public ControlPoint? ControlPoint { get; set; }
        
        public Guid UserId { get; set; }
        public User? User { get; set; }
        
        // AJOUT : Relation Customer pour multi-tenant
        public Guid CustomerId { get; set; }
        public Customer? Customer { get; set; }
        
        // Timestamps
        public DateTime ScannedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }

        // Double Bornage - Horodatage des 2 scans NFC
        public DateTime? FirstScanAt { get; set; }    // Premier scan (ouverture de tâche)
        public DateTime? SecondScanAt { get; set; }   // Second scan (validation finale)
        
        // Données saisies
        public string FormData { get; set; } = "{}";
        public string? PhotoUrl { get; set; }
        
        // Flags anti-triche (noms corrigés)
        public bool FlagSuspiciousValue { get; set; } = false;  // Valeur suspecte
        public bool FlagValeurRepetee { get; set; } = false;     // Valeur répétée
        public bool FlagSaisieRapide { get; set; } = false;      // Saisie trop rapide
        public bool FlagQuickEntry { get; set; } = false;        // Saisie rapide (doublon)
        public bool FlagOutOfRange { get; set; } = false;        // Hors plage
        public bool FlagHorsMarge { get; set; } = false;         // Hors marge
        public bool FlagSaisieDifferee { get; set; } = false;    // Saisie différée
        public bool FlagEcartOcr { get; set; } = false;          // Écart OCR
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}