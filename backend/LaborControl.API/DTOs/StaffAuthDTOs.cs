using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.DTOs
{
    /// <summary>
    /// Requête de login pour staff Labor Control
    /// </summary>
    public class StaffLoginRequest
    {
        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format email invalide")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est requis")]
        [MinLength(6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères")]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Réponse de login pour staff
    /// </summary>
    public class StaffLoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public bool RequiresPasswordChange { get; set; }
        public StaffUserDto User { get; set; } = new();
    }

    /// <summary>
    /// DTO pour utilisateur staff
    /// </summary>
    public class StaffUserDto
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? Department { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLoginAt { get; set; }
    }

    /// <summary>
    /// Requête de création de compte staff (SuperAdmin uniquement)
    /// Un email d'invitation avec lien sécurisé sera envoyé au collaborateur
    /// </summary>
    public class CreateStaffUserRequest
    {
        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format email invalide")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom est requis")]
        [MaxLength(100)]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le prénom est requis")]
        [MaxLength(100)]
        public string Prenom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le rôle est requis")]
        [RegularExpression("^(SUPERADMIN|ADMIN_STAFF|TECH|COMMERCIAL|ACHAT|COMPTA)$",
            ErrorMessage = "Rôle invalide")]
        public string Role { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Department { get; set; }
    }

    /// <summary>
    /// Requête de changement de mot de passe
    /// </summary>
    public class ChangeStaffPasswordRequest
    {
        [Required(ErrorMessage = "L'ancien mot de passe est requis")]
        public string OldPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nouveau mot de passe est requis")]
        [MinLength(6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmation du mot de passe est requise")]
        [Compare("NewPassword", ErrorMessage = "Les mots de passe ne correspondent pas")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Requête d'initialisation du compte owner (premier compte uniquement)
    /// </summary>
    public class InitializeOwnerRequest
    {
        [Required(ErrorMessage = "L'email est requis")]
        [EmailAddress(ErrorMessage = "Format email invalide")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom est requis")]
        [MaxLength(100)]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le prénom est requis")]
        [MaxLength(100)]
        public string Prenom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est requis")]
        [MinLength(8, ErrorMessage = "Le mot de passe doit contenir au moins 8 caractères")]
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>
    /// Requête de mise à jour d'un utilisateur staff
    /// </summary>
    public class UpdateStaffUserRequest
    {
        [Required(ErrorMessage = "Le nom est requis")]
        [MaxLength(100)]
        public string Nom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le prénom est requis")]
        [MaxLength(100)]
        public string Prenom { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le rôle est requis")]
        [RegularExpression("^(SUPERADMIN|ADMIN_STAFF|TECH|COMMERCIAL|ACHAT|COMPTA)$",
            ErrorMessage = "Rôle invalide")]
        public string Role { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? Department { get; set; }
    }

    /// <summary>
    /// Requête de réinitialisation de mot de passe (admin reset un utilisateur)
    /// </summary>
    public class ResetStaffPasswordRequest
    {
        [Required(ErrorMessage = "Le nouveau mot de passe est requis")]
        [MinLength(6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères")]
        public string NewPassword { get; set; } = string.Empty;

        public bool RequirePasswordChange { get; set; } = true;
    }

    /// <summary>
    /// Requête de définition de mot de passe via token d'invitation
    /// </summary>
    public class SetStaffPasswordRequest
    {
        [Required(ErrorMessage = "Le token est requis")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est requis")]
        [MinLength(8, ErrorMessage = "Le mot de passe doit contenir au moins 8 caractères")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmation du mot de passe est requise")]
        [Compare("Password", ErrorMessage = "Les mots de passe ne correspondent pas")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Réponse générique pour opérations staff
    /// </summary>
    public class StaffOperationResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public object? Data { get; set; }
    }
}
