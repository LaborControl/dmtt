using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    /// <summary>
    /// Association entre un utilisateur et ses qualifications
    /// Un technicien peut avoir plusieurs qualifications
    /// Gère les dates d'obtention, expiration et renouvellement
    /// </summary>
    public class UserQualification
    {
        public Guid Id { get; set; }

        [Required]
        public Guid UserId { get; set; }
        public User? User { get; set; }

        [Required]
        public Guid QualificationId { get; set; }
        public Qualification? Qualification { get; set; }

        /// <summary>
        /// Date d'obtention de la qualification
        /// </summary>
        [Required]
        public DateTime ObtainedDate { get; set; }

        /// <summary>
        /// Date d'expiration de la qualification (si applicable)
        /// </summary>
        public DateTime? ExpirationDate { get; set; }

        /// <summary>
        /// Numéro du certificat ou attestation
        /// </summary>
        [MaxLength(100)]
        public string? CertificateNumber { get; set; }

        /// <summary>
        /// Organisme qui a délivré cette qualification spécifique
        /// </summary>
        [MaxLength(200)]
        public string? IssuingOrganization { get; set; }

        /// <summary>
        /// Remarques ou notes (ex: "Renouvellement prévu en juin 2026")
        /// </summary>
        [MaxLength(500)]
        public string? Notes { get; set; }

        /// <summary>
        /// URL ou chemin vers le document justificatif (certificat scanné)
        /// </summary>
        [MaxLength(500)]
        public string? DocumentUrl { get; set; }

        /// <summary>
        /// Indique si la qualification est expirée
        /// </summary>
        public bool IsExpired => ExpirationDate.HasValue && ExpirationDate.Value < DateTime.UtcNow;

        /// <summary>
        /// Indique si la qualification expire bientôt (dans les 30 jours)
        /// </summary>
        public bool IsExpiringSoon => ExpirationDate.HasValue &&
                                      ExpirationDate.Value > DateTime.UtcNow &&
                                      ExpirationDate.Value <= DateTime.UtcNow.AddDays(30);

        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
