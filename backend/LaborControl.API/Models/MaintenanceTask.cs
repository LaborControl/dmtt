using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    /// <summary>
    /// Tâche spécifique dans une gamme de maintenance
    /// </summary>
    public class MaintenanceTask
    {
        public Guid Id { get; set; }

        [Required]
        public Guid MaintenanceScheduleId { get; set; }
        public MaintenanceSchedule? MaintenanceSchedule { get; set; }

        [Required]
        [MaxLength(200)]
        public string Name { get; set; } = string.Empty; // "Vérifier le niveau d'huile"

        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// Type de tâche : CHECK, REPLACE, CLEAN, LUBRICATE, ADJUST, MEASURE, CALIBRATE
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string TaskType { get; set; } = "CHECK";

        /// <summary>
        /// Ordre d'exécution dans la gamme
        /// </summary>
        public int OrderIndex { get; set; } = 0;

        /// <summary>
        /// Durée estimée pour cette tâche (en minutes)
        /// </summary>
        public int EstimatedDurationMinutes { get; set; } = 10;

        /// <summary>
        /// Instructions détaillées
        /// </summary>
        [MaxLength(2000)]
        public string? Instructions { get; set; }

        /// <summary>
        /// Valeurs limites ou critères d'acceptation
        /// Ex: "Niveau entre MIN et MAX", "Température < 80°C", "Pas de fuite visible"
        /// </summary>
        [MaxLength(500)]
        public string? AcceptanceCriteria { get; set; }

        /// <summary>
        /// Qualification spécifique requise pour cette tâche
        /// </summary>
        [MaxLength(50)]
        public string? SpecificQualification { get; set; }

        /// <summary>
        /// Outils spécifiques pour cette tâche (JSON)
        /// </summary>
        public string? SpecificTools { get; set; }

        /// <summary>
        /// Pièces/consommables pour cette tâche (JSON)
        /// </summary>
        public string? SpecificParts { get; set; }

        /// <summary>
        /// Tâche obligatoire ou optionnelle
        /// </summary>
        public bool IsMandatory { get; set; } = true;

        /// <summary>
        /// Nécessite une photo de validation
        /// </summary>
        public bool RequiresPhoto { get; set; } = false;

        /// <summary>
        /// Nécessite une mesure numérique
        /// </summary>
        public bool RequiresMeasurement { get; set; } = false;

        /// <summary>
        /// Unité de mesure si applicable
        /// </summary>
        [MaxLength(20)]
        public string? MeasurementUnit { get; set; }

        /// <summary>
        /// Valeur minimum acceptable
        /// </summary>
        public decimal? MinValue { get; set; }

        /// <summary>
        /// Valeur maximum acceptable
        /// </summary>
        public decimal? MaxValue { get; set; }

        /// <summary>
        /// Consignes de sécurité spécifiques
        /// </summary>
        [MaxLength(1000)]
        public string? SafetyInstructions { get; set; }

        /// <summary>
        /// Statut de la tâche : ACTIVE, INACTIVE
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string Status { get; set; } = "ACTIVE";

        /// <summary>
        /// Indique si cette tâche a été générée par l'IA
        /// </summary>
        public bool IsAiGenerated { get; set; } = false;

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}