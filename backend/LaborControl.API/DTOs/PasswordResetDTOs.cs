using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.DTOs
{
    /// <summary>
    /// Requête pour demander un reset de mot de passe
    /// </summary>
    public class ForgotPasswordRequest
    {
        [Required(ErrorMessage = "L'email ou le nom d'utilisateur est requis")]
        public string EmailOrUsername { get; set; } = string.Empty;
    }

    /// <summary>
    /// Réponse à une demande de reset de mot de passe
    /// </summary>
    public class ForgotPasswordResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string? TokenSentToEmail { get; set; }
    }

    /// <summary>
    /// Requête pour réinitialiser le mot de passe avec un token
    /// </summary>
    public class ResetPasswordRequest
    {
        [Required(ErrorMessage = "Le token est requis")]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nouveau mot de passe est requis")]
        [MinLength(6, ErrorMessage = "Le mot de passe doit contenir au moins 6 caractères")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "La confirmation du mot de passe est requise")]
        [Compare("NewPassword", ErrorMessage = "Les mots de passe ne correspondent pas")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }

    /// <summary>
    /// Réponse à une réinitialisation de mot de passe
    /// </summary>
    public class ResetPasswordResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
    }
}
