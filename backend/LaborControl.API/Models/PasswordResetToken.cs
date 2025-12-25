using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    /// <summary>
    /// Token de réinitialisation de mot de passe (pour Users et StaffUsers)
    /// </summary>
    public class PasswordResetToken
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Type d'utilisateur: "USER" pour clients, "STAFF" pour staff
        /// </summary>
        [Required]
        [MaxLength(20)]
        public string UserType { get; set; } = string.Empty;

        /// <summary>
        /// ID de l'utilisateur (User.Id ou StaffUser.Id)
        /// </summary>
        public Guid UserId { get; set; }

        /// <summary>
        /// Token unique généré pour la réinitialisation
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Token { get; set; } = string.Empty;

        /// <summary>
        /// Email ou Username utilisé pour la demande
        /// </summary>
        [MaxLength(255)]
        public string RequestedFor { get; set; } = string.Empty;

        /// <summary>
        /// Date de création du token
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date d'expiration du token (24h par défaut)
        /// </summary>
        public DateTime ExpiresAt { get; set; } = DateTime.UtcNow.AddHours(24);

        /// <summary>
        /// Indique si le token a déjà été utilisé
        /// </summary>
        public bool IsUsed { get; set; } = false;

        /// <summary>
        /// Date d'utilisation du token
        /// </summary>
        public DateTime? UsedAt { get; set; }
    }
}
