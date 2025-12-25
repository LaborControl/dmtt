using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    /// <summary>
    /// Association entre une gamme de maintenance (MaintenanceSchedule) et les qualifications requises
    /// Une gamme peut requérir plusieurs qualifications
    /// </summary>
    public class MaintenanceScheduleQualification
    {
        public Guid Id { get; set; }

        [Required]
        public Guid MaintenanceScheduleId { get; set; }
        public MaintenanceSchedule? MaintenanceSchedule { get; set; }

        [Required]
        public Guid QualificationId { get; set; }
        public Qualification? Qualification { get; set; }

        /// <summary>
        /// Indique si la qualification est obligatoire ou simplement recommandée
        /// </summary>
        public bool IsMandatory { get; set; } = true;

        /// <summary>
        /// Niveau d'alerte si qualification manquante
        /// 1 = Info, 2 = Warning, 3 = Bloquer l'affectation
        /// </summary>
        public int AlertLevel { get; set; } = 3;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
