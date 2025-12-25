using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    /// <summary>
    /// Utilisateur staff Labor Control (équipe interne)
    /// Complètement séparé des utilisateurs clients (table User)
    /// </summary>
    public class StaffUser
    {
        public Guid Id { get; set; }

        [MaxLength(255)]
        [Required]
        public string Email { get; set; } = string.Empty;

        [MaxLength(100)]
        [Required]
        public string Nom { get; set; } = string.Empty;

        [MaxLength(100)]
        [Required]
        public string Prenom { get; set; } = string.Empty;

        [Required]
        public string PasswordHash { get; set; } = string.Empty;

        /// <summary>
        /// Rôle staff : SUPERADMIN, ADMIN_STAFF, TECH, COMMERCIAL, ACHAT, COMPTA
        /// </summary>
        [MaxLength(50)]
        [Required]
        public string Role { get; set; } = "TECH";

        /// <summary>
        /// Département interne (optionnel)
        /// </summary>
        [MaxLength(100)]
        public string? Department { get; set; }

        /// <summary>
        /// Indique si l'utilisateur doit changer son mot de passe à la prochaine connexion
        /// </summary>
        public bool RequiresPasswordChange { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// Dernier login
        /// </summary>
        public DateTime? LastLoginAt { get; set; }

        /// <summary>
        /// Token de réinitialisation/définition de mot de passe
        /// </summary>
        [MaxLength(500)]
        public string? PasswordResetToken { get; set; }

        /// <summary>
        /// Date d'expiration du token (24h par défaut)
        /// </summary>
        public DateTime? PasswordResetTokenExpiry { get; set; }
    }
}
