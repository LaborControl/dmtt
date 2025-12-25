using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    public class User
    {
        public Guid Id { get; set; }

        [MaxLength(100)]
        public string Nom { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Prenom { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Tel { get; set; } = string.Empty;

        public string? Email { get; set; }

        /// <summary>
        /// Pseudo pour connexion (obligatoire pour non-admin, unique par customer)
        /// </summary>
        [MaxLength(50)]
        public string? Username { get; set; }

        /// <summary>
        /// Indique si l'utilisateur doit changer son mot de passe à la prochaine connexion
        /// </summary>
        public bool RequiresPasswordChange { get; set; } = false;

        /// <summary>
        /// Code PIN pour la première connexion (4 chiffres)
        /// </summary>
        [MaxLength(4)]
        public string? SetupPin { get; set; }

        /// <summary>
        /// Date d'expiration du PIN (72h après création)
        /// </summary>
        public DateTime? SetupPinExpiresAt { get; set; }

        /// <summary>
        /// Équipe à laquelle l'utilisateur appartient (optionnel)
        /// </summary>
        public Guid? TeamId { get; set; }
        public Team? Team { get; set; }

        /// <summary>
        /// Superviseur direct de l'utilisateur (optionnel)
        /// </summary>
        public Guid? SupervisorId { get; set; }
        public User? Supervisor { get; set; }

        /// <summary>
        /// Site auquel l'utilisateur est rattaché (optionnel, si pas dans une équipe)
        /// </summary>
        public Guid? SiteId { get; set; }
        public Site? Site { get; set; }

        /// <summary>
        /// Secteur d'activité de l'utilisateur
        /// </summary>
        public Guid? SectorId { get; set; }
        public Sector? Sector { get; set; }

        /// <summary>
        /// Métier de l'utilisateur
        /// </summary>
        public Guid? IndustryId { get; set; }
        public Industry? Industry { get; set; }

        [MaxLength(100)]
        public string Service { get; set; } = string.Empty;

        [MaxLength(100)]
        public string Fonction { get; set; } = string.Empty;

        [MaxLength(20)]
        public string Niveau { get; set; } = "User"; // User, Supervisor, Admin, SuperAdmin

        [MaxLength(20)]
        public string Role { get; set; } = "TECHNICIAN"; // Pour compatibilité

        public string PasswordHash { get; set; } = string.Empty;
        public Guid CustomerId { get; set; }
        public Customer? Customer { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        [MaxLength(100)]
        public string JobTitle { get; set; } = "Technicien de maintenance";

        public bool CanApproveDeviations { get; set; } = false;

        /// <summary>
        /// Indique si cet utilisateur est le propriétaire du compte (contact principal)
        /// true = contact client qui a créé le compte (Email obligatoire)
        /// false = employé du client (Email optionnel)
        /// </summary>
        public bool IsAccountOwner { get; set; } = false;

        // ========== PROPRIÉTÉS NUCLÉAIRES DMTT ==========

        /// <summary>
        /// Rôle nucléaire spécifique DMTT
        /// NONE, SUBCONTRACTOR, WELDER, NDT_CONTROLLER, CCPU, WELDING_COORDINATOR, QUALITY_MANAGER, EDF_INSPECTOR, PLANNER
        /// </summary>
        [MaxLength(30)]
        public string NuclearRole { get; set; } = "NONE";

        /// <summary>
        /// Entreprise sous-traitante (pour sous-traitants)
        /// </summary>
        [MaxLength(200)]
        public string? SubcontractorCompany { get; set; }

        /// <summary>
        /// Numéro de badge/matricule nucléaire
        /// </summary>
        [MaxLength(50)]
        public string? NuclearBadgeNumber { get; set; }

        /// <summary>
        /// Habilitation nucléaire valide
        /// </summary>
        public bool HasNuclearClearance { get; set; } = false;

        /// <summary>
        /// Date d'expiration habilitation nucléaire
        /// </summary>
        public DateTime? NuclearClearanceExpiresAt { get; set; }

        /// <summary>
        /// Peut exécuter des soudures nucléaires
        /// </summary>
        public bool CanPerformNuclearWelds { get; set; } = false;

        /// <summary>
        /// Peut effectuer des contrôles CND
        /// </summary>
        public bool CanPerformNDTControls { get; set; } = false;

        /// <summary>
        /// Peut valider en tant que CCPU
        /// </summary>
        public bool CanValidateAsCCPU { get; set; } = false;

        /// <summary>
        /// Peut valider les qualifications (Coordinateur soudage)
        /// </summary>
        public bool CanValidateQualifications { get; set; } = false;

        /// <summary>
        /// Peut valider les FNC et programmes CND (RQ)
        /// </summary>
        public bool CanValidateQuality { get; set; } = false;

        /// <summary>
        /// Peut effectuer la validation finale (Inspecteur EDF)
        /// </summary>
        public bool CanPerformFinalInspection { get; set; } = false;

        /// <summary>
        /// Peut gérer le planning (Planificateur)
        /// </summary>
        public bool CanManagePlanning { get; set; } = false;

        /// <summary>
        /// Niveau d'accès NFC (1-4)
        /// 1: Info simple, 2: +Hiérarchie, 3: +Fabrication, 4: +Historique complet
        /// </summary>
        public int NFCAccessLevel { get; set; } = 1;

        /// <summary>
        /// Date d'accès nucléaire accordé
        /// </summary>
        public DateTime? NuclearAccessGrantedAt { get; set; }

        /// <summary>
        /// Accordé par (ID utilisateur)
        /// </summary>
        public Guid? NuclearAccessGrantedById { get; set; }

        // Nouvelle relation many-to-many avec les qualifications
        public ICollection<UserQualification> UserQualifications { get; set; } = new List<UserQualification>();

        // Relations nucléaires
        public ICollection<WelderQualification> WelderQualifications { get; set; } = new List<WelderQualification>();
    }
}
