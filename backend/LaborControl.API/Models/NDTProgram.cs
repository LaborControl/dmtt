using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    /// <summary>
    /// NDTProgram - Programme de Contrôle Non Destructif
    /// Définit les contrôles CND requis pour un équipement/lot de soudures
    /// </summary>
    public class NDTProgram
    {
        public Guid Id { get; set; }

        [Required]
        public Guid CustomerId { get; set; }
        public Customer? Customer { get; set; }

        /// <summary>
        /// Référence du programme CND
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Reference { get; set; } = string.Empty;

        /// <summary>
        /// Nom/Titre du programme
        /// </summary>
        [Required]
        [MaxLength(300)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Version du document
        /// </summary>
        [MaxLength(20)]
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// Équipement concerné
        /// </summary>
        public Guid? AssetId { get; set; }
        public Asset? Asset { get; set; }

        /// <summary>
        /// Types de contrôles requis (JSON array)
        /// Ex: ["VT", "PT", "MT", "RT", "UT"]
        /// </summary>
        public string? RequiredControls { get; set; }

        /// <summary>
        /// Critères d'acceptation par type de contrôle (JSON)
        /// </summary>
        public string? AcceptanceCriteria { get; set; }

        /// <summary>
        /// Normes applicables (JSON array)
        /// Ex: ["EN ISO 17637", "EN ISO 3452", "RCC-M"]
        /// </summary>
        public string? ApplicableStandards { get; set; }

        /// <summary>
        /// Référence CDC ORANO
        /// </summary>
        [MaxLength(100)]
        public string? CDCReference { get; set; }

        /// <summary>
        /// Chemin vers le fichier PDF (Azure Blob)
        /// </summary>
        [MaxLength(500)]
        public string? FilePath { get; set; }

        /// <summary>
        /// Statut du programme
        /// DRAFT, PENDING_APPROVAL, APPROVED, SUPERSEDED, CANCELLED
        /// </summary>
        [MaxLength(30)]
        public string Status { get; set; } = "DRAFT";

        /// <summary>
        /// Généré par IA
        /// </summary>
        public bool GeneratedByAI { get; set; } = false;

        /// <summary>
        /// Version du modèle IA utilisé
        /// </summary>
        [MaxLength(50)]
        public string? AIModelVersion { get; set; }

        /// <summary>
        /// Prompt utilisé pour la génération IA
        /// </summary>
        public string? AIPrompt { get; set; }

        /// <summary>
        /// Données d'entrée utilisées pour la génération (JSON)
        /// </summary>
        public string? AIInputData { get; set; }

        /// <summary>
        /// Approuvé par (RQ)
        /// </summary>
        public Guid? ApprovedById { get; set; }
        public User? ApprovedBy { get; set; }

        /// <summary>
        /// Date d'approbation
        /// </summary>
        public DateTime? ApprovalDate { get; set; }

        /// <summary>
        /// Commentaires d'approbation
        /// </summary>
        [MaxLength(1000)]
        public string? ApprovalComments { get; set; }

        /// <summary>
        /// Taux d'échantillonnage (%) pour les contrôles
        /// </summary>
        public int SamplingRate { get; set; } = 100;

        /// <summary>
        /// Séquencement des contrôles (JSON)
        /// Définit l'ordre d'exécution des contrôles
        /// </summary>
        public string? ControlSequence { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Relations
        public ICollection<NDTControl> NDTControls { get; set; } = new List<NDTControl>();
    }
}
