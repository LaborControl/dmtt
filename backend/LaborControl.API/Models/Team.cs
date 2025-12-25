using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    /// <summary>
    /// Représente une équipe/service au sein d'une entreprise cliente.
    /// Permet la gestion multi-métiers : une entreprise peut avoir plusieurs équipes
    /// avec des secteurs d'activité différents (nettoyage, maintenance, restauration, etc.)
    /// </summary>
    public class Team
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Client propriétaire de cette équipe
        /// </summary>
        [Required]
        public Guid CustomerId { get; set; }
        public Customer? Customer { get; set; }

        /// <summary>
        /// Nom de l'équipe (ex: "Équipe Nettoyage Bâtiment A", "Brigade Maintenance Jour")
        /// </summary>
        [Required]
        [MaxLength(100)]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// Code court pour l'équipe (ex: "NET-A", "MAINT-01", "RESTO-PARIS")
        /// </summary>
        [MaxLength(50)]
        public string? Code { get; set; }

        /// <summary>
        /// Secteur d'activité / Métier de l'équipe
        /// Permet d'appliquer des templates métiers spécifiques
        /// </summary>
        public Guid? SectorId { get; set; }
        public Sector? Sector { get; set; }

        /// <summary>
        /// Description optionnelle de l'équipe
        /// </summary>
        [MaxLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// Responsable/Superviseur de l'équipe (optionnel)
        /// </summary>
        public Guid? SupervisorId { get; set; }
        public User? Supervisor { get; set; }

        /// <summary>
        /// Système de rotation de l'équipe (1x8, 2x8, 3x8)
        /// </summary>
        [MaxLength(10)]
        public string? WorkShiftSystem { get; set; }

        /// <summary>
        /// Fréquence de rotation (daily, weekly, biweekly, monthly, none)
        /// </summary>
        [MaxLength(20)]
        public string? RotationFrequency { get; set; }

        /// <summary>
        /// Indique si l'équipe travaille le samedi
        /// </summary>
        public bool WorksSaturday { get; set; } = false;

        /// <summary>
        /// Indique si l'équipe travaille le dimanche
        /// </summary>
        public bool WorksSunday { get; set; } = false;

        /// <summary>
        /// Indique si l'équipe effectue des astreintes
        /// </summary>
        public bool HasOnCallDuty { get; set; } = false;

        /// <summary>
        /// Pour créer une hiérarchie Service > Équipe
        /// Si ParentTeamId est null, c'est un Service (niveau 0)
        /// Si ParentTeamId est défini, c'est une Équipe appartenant à un Service (niveau 1)
        /// </summary>
        public Guid? ParentTeamId { get; set; }
        public Team? ParentTeam { get; set; }

        /// <summary>
        /// Niveau dans la hiérarchie (0 = Service racine, 1 = Équipe, etc.)
        /// </summary>
        public int Level { get; set; } = 0;

        /// <summary>
        /// Ordre d'affichage
        /// </summary>
        public int DisplayOrder { get; set; } = 0;

        /// <summary>
        /// Indique si l'équipe est active
        /// </summary>
        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Équipes enfants (sous-équipes ou équipes appartenant à un service)
        /// </summary>
        public ICollection<Team> SubTeams { get; set; } = new List<Team>();

        /// <summary>
        /// Membres de l'équipe (techniciens et superviseurs)
        /// </summary>
        public ICollection<User> Members { get; set; } = new List<User>();
    }
}
