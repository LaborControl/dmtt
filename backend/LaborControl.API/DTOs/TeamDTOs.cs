using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.DTOs
{
    /// <summary>
    /// DTO pour créer une nouvelle équipe
    /// </summary>
    public class CreateTeamRequest
    {
        [Required(ErrorMessage = "Le nom de l'équipe est obligatoire")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Code { get; set; }

        public Guid? SectorId { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public Guid? SiteId { get; set; }

        public Guid? SupervisorId { get; set; }

        [MaxLength(10)]
        public string? WorkShiftSystem { get; set; }

        [MaxLength(20)]
        public string? RotationFrequency { get; set; }

        public bool WorksSaturday { get; set; }

        public bool WorksSunday { get; set; }

        public bool HasOnCallDuty { get; set; }

        /// <summary>
        /// ID du service parent (null si c'est un Service de niveau 0)
        /// </summary>
        public Guid? ParentTeamId { get; set; }

        /// <summary>
        /// Niveau dans la hiérarchie (0 = Service, 1 = Équipe)
        /// </summary>
        public int Level { get; set; } = 0;

        /// <summary>
        /// Ordre d'affichage
        /// </summary>
        public int DisplayOrder { get; set; } = 0;
    }

    /// <summary>
    /// DTO pour mettre à jour une équipe existante
    /// </summary>
    public class UpdateTeamRequest
    {
        [Required(ErrorMessage = "Le nom de l'équipe est obligatoire")]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        [MaxLength(50)]
        public string? Code { get; set; }

        public Guid? SectorId { get; set; }

        [MaxLength(500)]
        public string? Description { get; set; }

        public Guid? SiteId { get; set; }

        public Guid? SupervisorId { get; set; }

        public bool IsActive { get; set; } = true;

        /// <summary>
        /// ID du service parent (null si c'est un Service de niveau 0)
        /// </summary>
        public Guid? ParentTeamId { get; set; }

        /// <summary>
        /// Niveau dans la hiérarchie (0 = Service, 1 = Équipe)
        /// </summary>
        public int Level { get; set; } = 0;

        /// <summary>
        /// Ordre d'affichage
        /// </summary>
        public int DisplayOrder { get; set; } = 0;
    }

    /// <summary>
    /// DTO de réponse pour une équipe
    /// </summary>
    public class TeamResponse
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public Guid? SectorId { get; set; }
        public string? SectorName { get; set; }
        public string? Description { get; set; }
        public Guid? SiteId { get; set; }
        public string? SiteName { get; set; }
        public Guid? SupervisorId { get; set; }
        public string? SupervisorName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public int MembersCount { get; set; }

        /// <summary>
        /// ID du service parent (null si c'est un Service de niveau 0)
        /// </summary>
        public Guid? ParentTeamId { get; set; }

        /// <summary>
        /// Niveau dans la hiérarchie (0 = Service, 1 = Équipe)
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Ordre d'affichage
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Nombre de sous-équipes
        /// </summary>
        public int SubTeamsCount { get; set; }
    }

    /// <summary>
    /// DTO de réponse détaillée pour une équipe (avec membres)
    /// </summary>
    public class TeamDetailResponse
    {
        public Guid Id { get; set; }
        public Guid CustomerId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public Guid? SectorId { get; set; }
        public string? SectorName { get; set; }
        public string? Description { get; set; }
        public Guid? SiteId { get; set; }
        public string? SiteName { get; set; }
        public Guid? SupervisorId { get; set; }
        public string? SupervisorName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<TeamMemberResponse> Members { get; set; } = new();

        /// <summary>
        /// ID du service parent (null si c'est un Service de niveau 0)
        /// </summary>
        public Guid? ParentTeamId { get; set; }

        /// <summary>
        /// Niveau dans la hiérarchie (0 = Service, 1 = Équipe)
        /// </summary>
        public int Level { get; set; }

        /// <summary>
        /// Ordre d'affichage
        /// </summary>
        public int DisplayOrder { get; set; }

        /// <summary>
        /// Sous-équipes
        /// </summary>
        public List<TeamResponse> SubTeams { get; set; } = new();
    }

    /// <summary>
    /// DTO pour un membre d'équipe
    /// </summary>
    public class TeamMemberResponse
    {
        public Guid Id { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Niveau { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public bool IsActive { get; set; }
    }

    /// <summary>
    /// DTO pour affecter un utilisateur à une équipe
    /// </summary>
    public class AssignUserToTeamRequest
    {
        [Required]
        public Guid UserId { get; set; }

        [Required]
        public Guid TeamId { get; set; }
    }

    /// <summary>
    /// DTO pour retirer un utilisateur d'une équipe
    /// </summary>
    public class RemoveUserFromTeamRequest
    {
        [Required]
        public Guid UserId { get; set; }
    }
}
