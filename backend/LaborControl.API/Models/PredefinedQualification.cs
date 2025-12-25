using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace LaborControl.API.Models
{
    /// <summary>
    /// Qualification prédéfinie (RNCP, RS, ou personnalisée)
    /// Peut appartenir à plusieurs secteurs d'activité
    /// </summary>
    public class PredefinedQualification
    {
        [Key]
        public Guid Id { get; set; }

        [Required]
        [StringLength(500)]
        public string Name { get; set; } = string.Empty;

        [StringLength(50)]
        public string? Code { get; set; }

        [StringLength(2000)]
        public string? Description { get; set; }

        // RNCP/RS Integration
        public QualificationType Type { get; set; } = QualificationType.Custom;

        [StringLength(20)]
        public string? RncpCode { get; set; } // Ex: "RNCP12345"

        [StringLength(20)]
        public string? RsCode { get; set; } // Ex: "RS1234"

        [StringLength(500)]
        public string? FranceCompetencesUrl { get; set; }

        public int? Level { get; set; } // Niveau de qualification (1-8)

        [StringLength(500)]
        public string? Certificateur { get; set; } // Organisme certificateur

        public DateTime? DateEnregistrement { get; set; }

        public DateTime? DateFinValidite { get; set; }

        // Metadata
        [StringLength(50)]
        public string? Color { get; set; }

        [StringLength(10)]
        public string? Icon { get; set; }

        public int DisplayOrder { get; set; } = 0;

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Many-to-Many relationship with PredefinedSectors
        public virtual ICollection<PredefinedQualificationSector> QualificationSectors { get; set; } = new List<PredefinedQualificationSector>();
    }

    public enum QualificationType
    {
        Custom = 0,      // Qualification personnalisée
        RNCP = 1,        // Répertoire National des Certifications Professionnelles
        RS = 2,          // Répertoire Spécifique
        CQP = 3,         // Certificat de Qualification Professionnelle
        Habilitation = 4 // Habilitation réglementaire (électrique, CACES, etc.)
    }

    /// <summary>
    /// Table de liaison Many-to-Many entre Qualifications et Secteurs
    /// </summary>
    public class PredefinedQualificationSector
    {
        public Guid PredefinedQualificationId { get; set; }

        [ForeignKey("PredefinedQualificationId")]
        public virtual PredefinedQualification PredefinedQualification { get; set; } = null!;

        public Guid PredefinedSectorId { get; set; }

        [ForeignKey("PredefinedSectorId")]
        public virtual PredefinedSector PredefinedSector { get; set; } = null!;
    }
}
