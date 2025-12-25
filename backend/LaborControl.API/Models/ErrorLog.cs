using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    /// <summary>
    /// Journal des erreurs applicatives signalées par les utilisateurs
    /// </summary>
    public class ErrorLog
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Message d'erreur affiché à l'utilisateur
        /// </summary>
        [MaxLength(500)]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// Stack trace complète de l'erreur (pour debugging)
        /// </summary>
        public string? StackTrace { get; set; }

        /// <summary>
        /// URL de la page où l'erreur s'est produite
        /// </summary>
        [MaxLength(500)]
        public string PageUrl { get; set; } = string.Empty;

        /// <summary>
        /// Données JSON additionnelles (contexte, requête, etc.)
        /// </summary>
        public string? AdditionalData { get; set; }

        /// <summary>
        /// Navigateur de l'utilisateur
        /// </summary>
        [MaxLength(200)]
        public string? UserAgent { get; set; }

        /// <summary>
        /// Email de l'utilisateur qui a rencontré l'erreur
        /// </summary>
        [MaxLength(100)]
        public string? UserEmail { get; set; }

        /// <summary>
        /// ID du client (si applicable)
        /// </summary>
        public Guid? CustomerId { get; set; }
        public Customer? Customer { get; set; }

        /// <summary>
        /// Type d'application: "CLIENT" ou "STAFF"
        /// </summary>
        [MaxLength(20)]
        public string AppType { get; set; } = "CLIENT";

        /// <summary>
        /// Gravité de l'erreur: "LOW", "MEDIUM", "HIGH", "CRITICAL"
        /// </summary>
        [MaxLength(20)]
        public string Severity { get; set; } = "MEDIUM";

        /// <summary>
        /// Statut de résolution: "PENDING", "IN_PROGRESS", "RESOLVED", "WONT_FIX"
        /// </summary>
        [MaxLength(20)]
        public string Status { get; set; } = "PENDING";

        /// <summary>
        /// Date de création de l'erreur
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date de résolution (si applicable)
        /// </summary>
        public DateTime? ResolvedAt { get; set; }

        /// <summary>
        /// Notes de résolution ou commentaires de l'IA
        /// </summary>
        public string? ResolutionNotes { get; set; }
    }
}
