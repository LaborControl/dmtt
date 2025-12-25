using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    /// <summary>
    /// NDTControl - Contrôle Non Destructif
    /// Enregistrement d'un contrôle CND effectué sur une soudure
    /// </summary>
    public class NDTControl
    {
        public Guid Id { get; set; }

        [Required]
        public Guid CustomerId { get; set; }
        public Customer? Customer { get; set; }

        /// <summary>
        /// Soudure contrôlée
        /// </summary>
        [Required]
        public Guid WeldId { get; set; }
        public Weld? Weld { get; set; }

        /// <summary>
        /// Programme CND de référence
        /// </summary>
        public Guid? NDTProgramId { get; set; }
        public NDTProgram? NDTProgram { get; set; }

        /// <summary>
        /// Type de contrôle
        /// VT (Visuel), PT (Ressuage), MT (Magnétoscopie), RT (Radiographie), UT (Ultrasons), ET (Courants de Foucault)
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string ControlType { get; set; } = "VT";

        /// <summary>
        /// Contrôleur CND
        /// </summary>
        public Guid? ControllerId { get; set; }
        public User? Controller { get; set; }

        /// <summary>
        /// Date de planification
        /// </summary>
        public DateTime? PlannedDate { get; set; }

        /// <summary>
        /// Date d'exécution
        /// </summary>
        public DateTime? ControlDate { get; set; }

        /// <summary>
        /// Résultat du contrôle
        /// PENDING, CONFORM, NON_CONFORM, CONDITIONAL
        /// </summary>
        [MaxLength(20)]
        public string Result { get; set; } = "PENDING";

        /// <summary>
        /// Niveau de qualification du contrôleur (1, 2, 3)
        /// </summary>
        public int? ControllerLevel { get; set; }

        /// <summary>
        /// Norme appliquée
        /// </summary>
        [MaxLength(100)]
        public string? AppliedStandard { get; set; }

        /// <summary>
        /// Critères d'acceptation utilisés
        /// </summary>
        [MaxLength(500)]
        public string? AcceptanceCriteria { get; set; }

        /// <summary>
        /// Commentaires / Observations
        /// </summary>
        [MaxLength(2000)]
        public string? Comments { get; set; }

        /// <summary>
        /// Défauts détectés (JSON array)
        /// Ex: [{"type": "porosité", "location": "racine", "size_mm": 1.5}]
        /// </summary>
        public string? DefectsFound { get; set; }

        /// <summary>
        /// Paramètres du contrôle (JSON)
        /// Ex pour UT: {"frequency": 4, "probe": "angle 60°", "coupling": "gel"}
        /// </summary>
        public string? ControlParameters { get; set; }

        /// <summary>
        /// Photos/Images du contrôle (JSON array d'URLs)
        /// </summary>
        public string? Photos { get; set; }

        /// <summary>
        /// Chemin vers le rapport PDF (Azure Blob)
        /// </summary>
        [MaxLength(500)]
        public string? ReportFilePath { get; set; }

        /// <summary>
        /// Conditions environnementales (JSON)
        /// Ex: {"temperature": 22, "humidity": 45, "lighting": "adequate"}
        /// </summary>
        public string? EnvironmentalConditions { get; set; }

        /// <summary>
        /// Équipement de contrôle utilisé
        /// </summary>
        [MaxLength(200)]
        public string? EquipmentUsed { get; set; }

        /// <summary>
        /// Numéro de certificat d'étalonnage de l'équipement
        /// </summary>
        [MaxLength(100)]
        public string? EquipmentCalibrationNumber { get; set; }

        /// <summary>
        /// Statut du contrôle
        /// PLANNED, IN_PROGRESS, COMPLETED, CANCELLED
        /// </summary>
        [MaxLength(20)]
        public string Status { get; set; } = "PLANNED";

        /// <summary>
        /// Premier scan NFC (début contrôle)
        /// </summary>
        public DateTime? FirstScanAt { get; set; }

        /// <summary>
        /// Second scan NFC (fin contrôle)
        /// </summary>
        public DateTime? SecondScanAt { get; set; }

        /// <summary>
        /// FNC associée si non-conforme
        /// </summary>
        public Guid? NonConformityId { get; set; }
        public NonConformity? NonConformity { get; set; }

        /// <summary>
        /// Signature numérique du contrôleur
        /// </summary>
        public string? ControllerSignature { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
