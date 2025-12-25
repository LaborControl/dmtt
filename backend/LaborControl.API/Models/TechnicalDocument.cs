using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    /// <summary>
    /// TechnicalDocument - Documents techniques du projet
    /// CDC, Plans BE, Normes, Certificats, Rapports, etc.
    /// </summary>
    public class TechnicalDocument
    {
        public Guid Id { get; set; }

        [Required]
        public Guid CustomerId { get; set; }
        public Customer? Customer { get; set; }

        /// <summary>
        /// Référence du document
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Reference { get; set; } = string.Empty;

        /// <summary>
        /// Nom du document
        /// </summary>
        [Required]
        [MaxLength(300)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Type de document
        /// CDC, PLAN_BE, NORM, CERTIFICATE, DMOS, NDT_PROGRAM, NDT_REPORT, PROCEDURE, REPORT, OTHER
        /// </summary>
        [Required]
        [MaxLength(30)]
        public string Type { get; set; } = "OTHER";

        /// <summary>
        /// Catégorie (pour organisation)
        /// </summary>
        [MaxLength(100)]
        public string? Category { get; set; }

        /// <summary>
        /// Description
        /// </summary>
        [MaxLength(1000)]
        public string? Description { get; set; }

        /// <summary>
        /// Version du document
        /// </summary>
        [MaxLength(20)]
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// Indice de révision
        /// </summary>
        [MaxLength(10)]
        public string? RevisionIndex { get; set; }

        /// <summary>
        /// Chemin vers le fichier (Azure Blob)
        /// </summary>
        [Required]
        [MaxLength(500)]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// Nom du fichier original
        /// </summary>
        [MaxLength(300)]
        public string? OriginalFileName { get; set; }

        /// <summary>
        /// Type MIME
        /// </summary>
        [MaxLength(100)]
        public string? MimeType { get; set; }

        /// <summary>
        /// Taille du fichier (bytes)
        /// </summary>
        public long? FileSize { get; set; }

        /// <summary>
        /// Équipement/Élément associé
        /// </summary>
        public Guid? AssetId { get; set; }
        public Asset? Asset { get; set; }

        /// <summary>
        /// Soudure associée
        /// </summary>
        public Guid? WeldId { get; set; }
        public Weld? Weld { get; set; }

        /// <summary>
        /// Uploadé par
        /// </summary>
        [Required]
        public Guid UploadedById { get; set; }
        public User? UploadedBy { get; set; }

        /// <summary>
        /// Date d'upload
        /// </summary>
        public DateTime UploadDate { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date du document
        /// </summary>
        public DateTime? DocumentDate { get; set; }

        /// <summary>
        /// Date de validité (pour normes/certificats)
        /// </summary>
        public DateTime? ValidUntil { get; set; }

        /// <summary>
        /// Statut du document
        /// DRAFT, PENDING_REVIEW, APPROVED, SUPERSEDED, ARCHIVED
        /// </summary>
        [MaxLength(30)]
        public string Status { get; set; } = "DRAFT";

        /// <summary>
        /// Approuvé par
        /// </summary>
        public Guid? ApprovedById { get; set; }
        public User? ApprovedBy { get; set; }

        /// <summary>
        /// Date d'approbation
        /// </summary>
        public DateTime? ApprovalDate { get; set; }

        /// <summary>
        /// Émetteur du document
        /// </summary>
        [MaxLength(200)]
        public string? Issuer { get; set; }

        /// <summary>
        /// Tags/Mots-clés (JSON array)
        /// </summary>
        public string? Tags { get; set; }

        /// <summary>
        /// Métadonnées extraites par IA (JSON)
        /// </summary>
        public string? AIExtractedMetadata { get; set; }

        /// <summary>
        /// Contenu textuel extrait (pour recherche)
        /// </summary>
        public string? ExtractedText { get; set; }

        /// <summary>
        /// Document analysé par IA
        /// </summary>
        public bool AnalyzedByAI { get; set; } = false;

        /// <summary>
        /// Confidentiel (accès restreint)
        /// </summary>
        public bool IsConfidential { get; set; } = false;

        /// <summary>
        /// Niveau d'accès requis (JSON array de rôles)
        /// </summary>
        public string? AccessRoles { get; set; }

        /// <summary>
        /// Nombre de téléchargements
        /// </summary>
        public int DownloadCount { get; set; } = 0;

        /// <summary>
        /// Dernier accès
        /// </summary>
        public DateTime? LastAccessedAt { get; set; }

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
