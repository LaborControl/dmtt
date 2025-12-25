using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    /// <summary>
    /// Association entre un protocole d'intervention (TaskTemplate) et les qualifications requises
    /// Un protocole peut requérir plusieurs qualifications
    /// </summary>
    public class TaskTemplateQualification
    {
        public Guid Id { get; set; }

        [Required]
        public Guid TaskTemplateId { get; set; }
        public TaskTemplate? TaskTemplate { get; set; }

        [Required]
        public Guid QualificationId { get; set; }
        public Qualification? Qualification { get; set; }

        /// <summary>
        /// Indique si la qualification est obligatoire ou simplement recommandée
        /// </summary>
        public bool IsMandatory { get; set; } = true;

        /// <summary>
        /// Alternative : si la qualification principale n'est pas disponible,
        /// ces autres qualifications peuvent suffire
        /// Ex: "B2V" peut être remplacé par "BR" dans certains cas
        /// </summary>
        [MaxLength(500)]
        public string? AlternativeQualificationIds { get; set; } // JSON array de Guid

        /// <summary>
        /// Niveau d'alerte si qualification manquante
        /// 1 = Info, 2 = Warning, 3 = Bloquer l'affectation
        /// </summary>
        public int AlertLevel { get; set; } = 3;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
