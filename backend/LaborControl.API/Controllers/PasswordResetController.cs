using LaborControl.API.Data;
using LaborControl.API.DTOs;
using LaborControl.API.Models;
using LaborControl.API.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace LaborControl.API.Controllers
{
    [ApiController]
    [Route("api/password-reset")]
    public class PasswordResetController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<PasswordResetController> _logger;
        private readonly IConfiguration _configuration;

        public PasswordResetController(
            ApplicationDbContext context,
            IEmailService emailService,
            ILogger<PasswordResetController> logger,
            IConfiguration configuration)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Demande de réinitialisation de mot de passe pour un utilisateur client
        /// </summary>
        [HttpPost("forgot-client")]
        public async Task<ActionResult<ForgotPasswordResponse>> ForgotPasswordClient([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                // Chercher l'utilisateur par email ou username
                var user = await _context.Users
                    .Include(u => u.Supervisor)
                    .Include(u => u.Customer)
                    .FirstOrDefaultAsync(u => u.Email == request.EmailOrUsername || u.Username == request.EmailOrUsername);

                if (user == null)
                {
                    // Pour des raisons de sécurité, on ne révèle pas si l'utilisateur existe ou non
                    return Ok(new ForgotPasswordResponse
                    {
                        Success = true,
                        Message = "Si un compte existe avec cet identifiant, un email de réinitialisation a été envoyé."
                    });
                }

                // Générer un token unique
                var token = Guid.NewGuid().ToString("N");

                // Créer l'entrée de token
                var resetToken = new PasswordResetToken
                {
                    Id = Guid.NewGuid(),
                    UserType = "USER",
                    UserId = user.Id,
                    Token = token,
                    RequestedFor = request.EmailOrUsername,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    IsUsed = false
                };

                _context.PasswordResetTokens.Add(resetToken);
                await _context.SaveChangesAsync();

                // Déterminer si l'utilisateur a un email
                if (!string.IsNullOrEmpty(user.Email))
                {
                    // L'utilisateur a un email, envoyer le lien de reset directement
                    var clientAppBaseUrl = Environment.GetEnvironmentVariable("AppSettings__ClientAppUrl")
                        ?? _configuration["AppSettings:ClientAppUrl"]
                        ?? "https://app.labor-control.fr";
                    var resetLink = $"{clientAppBaseUrl}/reset-password?token={token}";
                    var userName = $"{user.Prenom} {user.Nom}";

                    await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);

                    _logger.LogInformation($"Email de réinitialisation envoyé à {user.Email} pour l'utilisateur {user.Id}");

                    return Ok(new ForgotPasswordResponse
                    {
                        Success = true,
                        Message = "Un email de réinitialisation a été envoyé à votre adresse.",
                        TokenSentToEmail = MaskEmail(user.Email)
                    });
                }
                else
                {
                    // L'utilisateur n'a pas d'email, chercher le superviseur ou contact client
                    string? notificationEmail = null;
                    string? notificationName = null;

                    if (user.Supervisor != null && !string.IsNullOrEmpty(user.Supervisor.Email))
                    {
                        notificationEmail = user.Supervisor.Email;
                        notificationName = $"{user.Supervisor.Prenom} {user.Supervisor.Nom}";
                    }
                    else if (user.Customer != null && !string.IsNullOrEmpty(user.Customer.ContactEmail))
                    {
                        notificationEmail = user.Customer.ContactEmail;
                        notificationName = user.Customer.Name;
                    }

                    if (!string.IsNullOrEmpty(notificationEmail))
                    {
                        var userName = !string.IsNullOrEmpty(user.Username) ? user.Username : $"{user.Prenom} {user.Nom}";
                        await _emailService.SendSupervisorPasswordResetNotificationAsync(notificationEmail, userName);

                        _logger.LogInformation($"Notification envoyée à {notificationEmail} pour réinitialisation du mot de passe de l'utilisateur {user.Id}");

                        return Ok(new ForgotPasswordResponse
                        {
                            Success = true,
                            Message = "Une notification a été envoyée à votre superviseur pour réinitialiser votre mot de passe.",
                            TokenSentToEmail = MaskEmail(notificationEmail)
                        });
                    }
                    else
                    {
                        _logger.LogWarning($"Aucun email trouvé pour l'utilisateur {user.Id} ou son superviseur");

                        return Ok(new ForgotPasswordResponse
                        {
                            Success = false,
                            Message = "Impossible d'envoyer un email de réinitialisation. Veuillez contacter votre administrateur."
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la demande de réinitialisation de mot de passe client");
                return StatusCode(500, new ForgotPasswordResponse
                {
                    Success = false,
                    Message = "Une erreur s'est produite. Veuillez réessayer plus tard."
                });
            }
        }

        /// <summary>
        /// Réinitialisation du mot de passe pour un utilisateur client avec un token
        /// </summary>
        [HttpPost("reset-client")]
        public async Task<ActionResult<ResetPasswordResponse>> ResetPasswordClient([FromBody] ResetPasswordRequest request)
        {
            try
            {
                // Valider le token
                var resetToken = await _context.PasswordResetTokens
                    .FirstOrDefaultAsync(t => t.Token == request.Token && t.UserType == "USER");

                if (resetToken == null)
                {
                    return BadRequest(new ResetPasswordResponse
                    {
                        Success = false,
                        Message = "Token invalide."
                    });
                }

                // Vérifier si le token a expiré
                if (resetToken.ExpiresAt < DateTime.UtcNow)
                {
                    return BadRequest(new ResetPasswordResponse
                    {
                        Success = false,
                        Message = "Ce token a expiré. Veuillez refaire une demande de réinitialisation."
                    });
                }

                // Vérifier si le token a déjà été utilisé
                if (resetToken.IsUsed)
                {
                    return BadRequest(new ResetPasswordResponse
                    {
                        Success = false,
                        Message = "Ce token a déjà été utilisé."
                    });
                }

                // Récupérer l'utilisateur
                var user = await _context.Users.FindAsync(resetToken.UserId);
                if (user == null)
                {
                    return NotFound(new ResetPasswordResponse
                    {
                        Success = false,
                        Message = "Utilisateur non trouvé."
                    });
                }

                // Hasher le nouveau mot de passe
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

                // Marquer le token comme utilisé
                resetToken.IsUsed = true;
                resetToken.UsedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Mot de passe réinitialisé avec succès pour l'utilisateur {user.Id}");

                return Ok(new ResetPasswordResponse
                {
                    Success = true,
                    Message = "Votre mot de passe a été réinitialisé avec succès."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la réinitialisation du mot de passe client");
                return StatusCode(500, new ResetPasswordResponse
                {
                    Success = false,
                    Message = "Une erreur s'est produite. Veuillez réessayer plus tard."
                });
            }
        }

        /// <summary>
        /// Demande de réinitialisation de mot de passe pour un utilisateur staff
        /// </summary>
        [HttpPost("forgot-staff")]
        public async Task<ActionResult<ForgotPasswordResponse>> ForgotPasswordStaff([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                // Chercher l'utilisateur staff par email
                var staffUser = await _context.StaffUsers
                    .FirstOrDefaultAsync(u => u.Email == request.EmailOrUsername);

                if (staffUser == null)
                {
                    // Pour des raisons de sécurité, on ne révèle pas si l'utilisateur existe ou non
                    return Ok(new ForgotPasswordResponse
                    {
                        Success = true,
                        Message = "Si un compte existe avec cet email, un email de réinitialisation a été envoyé."
                    });
                }

                // Générer un token unique
                var token = Guid.NewGuid().ToString("N");

                // Créer l'entrée de token
                var resetToken = new PasswordResetToken
                {
                    Id = Guid.NewGuid(),
                    UserType = "STAFF",
                    UserId = staffUser.Id,
                    Token = token,
                    RequestedFor = request.EmailOrUsername,
                    CreatedAt = DateTime.UtcNow,
                    ExpiresAt = DateTime.UtcNow.AddHours(24),
                    IsUsed = false
                };

                _context.PasswordResetTokens.Add(resetToken);
                await _context.SaveChangesAsync();

                // Envoyer l'email de réinitialisation
                var staffAppBaseUrl = Environment.GetEnvironmentVariable("AppSettings__StaffAppUrl")
                    ?? _configuration["AppSettings:StaffAppUrl"]
                    ?? "https://gestion.labor-control.fr";
                var resetLink = $"{staffAppBaseUrl}/gestion/reset-password?token={token}";
                await _emailService.SendPasswordResetEmailAsync(staffUser.Email, resetLink);

                _logger.LogInformation($"Email de réinitialisation envoyé à {staffUser.Email} pour le staff {staffUser.Id}");

                return Ok(new ForgotPasswordResponse
                {
                    Success = true,
                    Message = "Un email de réinitialisation a été envoyé à votre adresse.",
                    TokenSentToEmail = MaskEmail(staffUser.Email)
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la demande de réinitialisation de mot de passe staff");
                return StatusCode(500, new ForgotPasswordResponse
                {
                    Success = false,
                    Message = "Une erreur s'est produite. Veuillez réessayer plus tard."
                });
            }
        }

        /// <summary>
        /// Réinitialisation du mot de passe pour un utilisateur staff avec un token
        /// </summary>
        [HttpPost("reset-staff")]
        public async Task<ActionResult<ResetPasswordResponse>> ResetPasswordStaff([FromBody] ResetPasswordRequest request)
        {
            try
            {
                // Valider le token
                var resetToken = await _context.PasswordResetTokens
                    .FirstOrDefaultAsync(t => t.Token == request.Token && t.UserType == "STAFF");

                if (resetToken == null)
                {
                    return BadRequest(new ResetPasswordResponse
                    {
                        Success = false,
                        Message = "Token invalide."
                    });
                }

                // Vérifier si le token a expiré
                if (resetToken.ExpiresAt < DateTime.UtcNow)
                {
                    return BadRequest(new ResetPasswordResponse
                    {
                        Success = false,
                        Message = "Ce token a expiré. Veuillez refaire une demande de réinitialisation."
                    });
                }

                // Vérifier si le token a déjà été utilisé
                if (resetToken.IsUsed)
                {
                    return BadRequest(new ResetPasswordResponse
                    {
                        Success = false,
                        Message = "Ce token a déjà été utilisé."
                    });
                }

                // Récupérer l'utilisateur staff
                var staffUser = await _context.StaffUsers.FindAsync(resetToken.UserId);
                if (staffUser == null)
                {
                    return NotFound(new ResetPasswordResponse
                    {
                        Success = false,
                        Message = "Utilisateur non trouvé."
                    });
                }

                // Hasher le nouveau mot de passe
                staffUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);

                // Réinitialiser le flag de changement de mot de passe
                staffUser.RequiresPasswordChange = false;

                // Marquer le token comme utilisé
                resetToken.IsUsed = true;
                resetToken.UsedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"Mot de passe réinitialisé avec succès pour le staff {staffUser.Id}");

                return Ok(new ResetPasswordResponse
                {
                    Success = true,
                    Message = "Votre mot de passe a été réinitialisé avec succès."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la réinitialisation du mot de passe staff");
                return StatusCode(500, new ResetPasswordResponse
                {
                    Success = false,
                    Message = "Une erreur s'est produite. Veuillez réessayer plus tard."
                });
            }
        }

        /// <summary>
        /// Masque partiellement un email pour la sécurité
        /// </summary>
        private string MaskEmail(string email)
        {
            if (string.IsNullOrEmpty(email))
                return string.Empty;

            var parts = email.Split('@');
            if (parts.Length != 2)
                return email;

            var localPart = parts[0];
            var domain = parts[1];

            if (localPart.Length <= 2)
                return $"{localPart[0]}***@{domain}";

            return $"{localPart[0]}***{localPart[^1]}@{domain}";
        }
    }
}
