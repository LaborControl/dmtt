using LaborControl.API.Data;
using LaborControl.API.DTOs;
using LaborControl.API.Models;
using LaborControl.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace LaborControl.API.Controllers
{
    [ApiController]
    [Route("api/staff-auth")]
    public class StaffAuthController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly ILogger<StaffAuthController> _logger;
        private readonly IEmailService _emailService;

        public StaffAuthController(
            ApplicationDbContext context,
            IConfiguration configuration,
            ILogger<StaffAuthController> logger,
            IEmailService emailService)
        {
            _context = context;
            _configuration = configuration;
            _logger = logger;
            _emailService = emailService;
        }

        /// <summary>
        /// Login pour staff Labor Control
        /// </summary>
        [HttpPost("login")]
        public async Task<ActionResult<StaffLoginResponse>> Login(StaffLoginRequest request)
        {
            try
            {
                // Optimisation: AsNoTracking pour lecture seule lors du login
                var staffUser = await _context.StaffUsers
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u => u.Email == request.Email && u.IsActive);

                if (staffUser == null)
                {
                    _logger.LogWarning($"[STAFF LOGIN] Tentative de connexion échouée pour {request.Email} - utilisateur non trouvé");
                    return Unauthorized(new { message = "Email ou mot de passe incorrect" });
                }

                // Vérifier si l'utilisateur a défini un mot de passe
                if (string.IsNullOrEmpty(staffUser.PasswordHash))
                {
                    _logger.LogWarning($"[STAFF LOGIN] Tentative de connexion pour {request.Email} - compte non activé");
                    return Unauthorized(new { message = "Compte non activé. Veuillez consulter l'email d'invitation pour définir votre mot de passe." });
                }

                // Vérifier le mot de passe
                bool passwordValid = BCrypt.Net.BCrypt.Verify(request.Password, staffUser.PasswordHash);

                if (!passwordValid)
                {
                    _logger.LogWarning($"[STAFF LOGIN] Tentative de connexion échouée pour {request.Email} - mot de passe incorrect");
                    return Unauthorized(new { message = "Email ou mot de passe incorrect" });
                }

                // Générer le token JWT
                var token = GenerateJwtToken(staffUser);

                // Optimisation: LastLoginAt non mis à jour pour améliorer les performances
                // Cette écriture en base ralentissait significativement les temps de connexion
                // Si nécessaire, cette métrique peut être ajoutée via un job en arrière-plan

                _logger.LogInformation($"[STAFF LOGIN] Connexion réussie pour {staffUser.Email} (Rôle: {staffUser.Role})");

                return Ok(new StaffLoginResponse
                {
                    Token = token,
                    RequiresPasswordChange = staffUser.RequiresPasswordChange,
                    User = new StaffUserDto
                    {
                        Id = staffUser.Id,
                        Email = staffUser.Email,
                        Nom = staffUser.Nom,
                        Prenom = staffUser.Prenom,
                        Role = staffUser.Role,
                        Department = staffUser.Department,
                        CreatedAt = staffUser.CreatedAt,
                        IsActive = staffUser.IsActive,
                        LastLoginAt = staffUser.LastLoginAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[STAFF LOGIN] Erreur lors de la connexion");
                return StatusCode(500, new { message = "Erreur serveur lors de la connexion" });
            }
        }

        /// <summary>
        /// Créer un nouvel utilisateur staff
        /// </summary>
        [HttpPost("create-staff")]
        [Authorize]
        public async Task<ActionResult<StaffOperationResponse>> CreateStaffUser(CreateStaffUserRequest request)
        {
            try
            {

                // Vérifier que l'email n'existe pas déjà
                var existingUser = await _context.StaffUsers
                    .FirstOrDefaultAsync(u => u.Email == request.Email);

                if (existingUser != null)
                {
                    return BadRequest(new StaffOperationResponse
                    {
                        Success = false,
                        Message = "Un compte staff existe déjà avec cet email"
                    });
                }

                // Générer un token sécurisé pour la définition du mot de passe
                var tokenBytes = new byte[64];
                using (var rng = RandomNumberGenerator.Create())
                {
                    rng.GetBytes(tokenBytes);
                }
                var token = Convert.ToBase64String(tokenBytes);

                // Créer le nouvel utilisateur staff sans mot de passe
                var staffUser = new StaffUser
                {
                    Id = Guid.NewGuid(),
                    Email = request.Email,
                    Nom = request.Nom,
                    Prenom = request.Prenom,
                    Role = request.Role,
                    Department = request.Department,
                    PasswordHash = "", // Pas de mot de passe défini, l'utilisateur le définira via l'email
                    PasswordResetToken = token,
                    PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(24), // Token valable 24h
                    RequiresPasswordChange = true,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.StaffUsers.Add(staffUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"[STAFF CREATE] Nouvel utilisateur staff créé: {request.Email} (Rôle: {request.Role})");

                // Envoyer l'email d'invitation
                try
                {
                    var emailSent = await _emailService.SendStaffInvitationEmailAsync(
                        staffUser.Email,
                        staffUser.Prenom,
                        staffUser.Nom,
                        token
                    );

                    if (emailSent)
                    {
                        _logger.LogInformation($"[STAFF CREATE] Email d'invitation envoyé à {request.Email}");
                    }
                    else
                    {
                        _logger.LogWarning($"[STAFF CREATE] Échec de l'envoi de l'email d'invitation à {request.Email}");
                    }
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, $"[STAFF CREATE] Erreur lors de l'envoi de l'email d'invitation à {request.Email}");
                }

                return Ok(new StaffOperationResponse
                {
                    Success = true,
                    Message = "Utilisateur staff créé avec succès. Un email d'invitation a été envoyé.",
                    Data = new StaffUserDto
                    {
                        Id = staffUser.Id,
                        Email = staffUser.Email,
                        Nom = staffUser.Nom,
                        Prenom = staffUser.Prenom,
                        Role = staffUser.Role,
                        Department = staffUser.Department,
                        CreatedAt = staffUser.CreatedAt,
                        IsActive = staffUser.IsActive
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[STAFF CREATE] Erreur lors de la création d'utilisateur staff");
                return StatusCode(500, new StaffOperationResponse
                {
                    Success = false,
                    Message = "Erreur serveur lors de la création"
                });
            }
        }

        /// <summary>
        /// Changer le mot de passe (staff authentifié)
        /// </summary>
        [HttpPost("change-password")]
        [Authorize]
        public async Task<ActionResult<StaffOperationResponse>> ChangePassword(ChangeStaffPasswordRequest request)
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");
                var staffUser = await _context.StaffUsers.FindAsync(userId);

                if (staffUser == null)
                {
                    return NotFound(new StaffOperationResponse
                    {
                        Success = false,
                        Message = "Utilisateur non trouvé"
                    });
                }

                // Vérifier l'ancien mot de passe
                bool oldPasswordValid = BCrypt.Net.BCrypt.Verify(request.OldPassword, staffUser.PasswordHash);

                if (!oldPasswordValid)
                {
                    return BadRequest(new StaffOperationResponse
                    {
                        Success = false,
                        Message = "Ancien mot de passe incorrect"
                    });
                }

                // Mettre à jour le mot de passe
                staffUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                staffUser.RequiresPasswordChange = false;
                staffUser.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"[STAFF PASSWORD] Mot de passe changé pour {staffUser.Email}");

                return Ok(new StaffOperationResponse
                {
                    Success = true,
                    Message = "Mot de passe changé avec succès"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[STAFF PASSWORD] Erreur lors du changement de mot de passe");
                return StatusCode(500, new StaffOperationResponse
                {
                    Success = false,
                    Message = "Erreur serveur lors du changement de mot de passe"
                });
            }
        }

        /// <summary>
        /// Définir le mot de passe via token d'invitation
        /// </summary>
        [HttpPost("set-password")]
        public async Task<ActionResult<StaffOperationResponse>> SetPassword([FromBody] SetStaffPasswordRequest request)
        {
            try
            {
                // Trouver l'utilisateur par token
                var staffUser = await _context.StaffUsers
                    .FirstOrDefaultAsync(u => u.PasswordResetToken == request.Token);

                if (staffUser == null)
                {
                    return BadRequest(new StaffOperationResponse
                    {
                        Success = false,
                        Message = "Token invalide"
                    });
                }

                // Vérifier l'expiration du token
                if (staffUser.PasswordResetTokenExpiry == null || staffUser.PasswordResetTokenExpiry < DateTime.UtcNow)
                {
                    return BadRequest(new StaffOperationResponse
                    {
                        Success = false,
                        Message = "Le lien a expiré. Veuillez contacter votre administrateur pour obtenir un nouveau lien."
                    });
                }

                // Valider le mot de passe (8 car min, 1 maj, 1 chiffre, 1 spécial)
                if (!IsPasswordStrong(request.Password))
                {
                    return BadRequest(new StaffOperationResponse
                    {
                        Success = false,
                        Message = "Le mot de passe doit contenir au minimum 8 caractères, dont au moins une majuscule, un chiffre et un caractère spécial (!@#$%^&*...)."
                    });
                }

                // Définir le mot de passe
                staffUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);
                staffUser.PasswordResetToken = null;
                staffUser.PasswordResetTokenExpiry = null;
                staffUser.RequiresPasswordChange = false;
                staffUser.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"[STAFF SET PASSWORD] Mot de passe défini pour {staffUser.Email}");

                return Ok(new StaffOperationResponse
                {
                    Success = true,
                    Message = "Mot de passe défini avec succès. Vous pouvez maintenant vous connecter."
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[STAFF SET PASSWORD] Erreur lors de la définition du mot de passe");
                return StatusCode(500, new StaffOperationResponse
                {
                    Success = false,
                    Message = "Erreur serveur lors de la définition du mot de passe"
                });
            }
        }

        /// <summary>
        /// Valide si un mot de passe est fort (8 car min, 1 maj, 1 chiffre, 1 spécial)
        /// </summary>
        private bool IsPasswordStrong(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8)
                return false;

            bool hasUpperCase = password.Any(char.IsUpper);
            bool hasDigit = password.Any(char.IsDigit);
            bool hasSpecialChar = password.Any(ch => !char.IsLetterOrDigit(ch));

            return hasUpperCase && hasDigit && hasSpecialChar;
        }

        /// <summary>
        /// Récupérer les informations du staff connecté
        /// </summary>
        [HttpGet("me")]
        [Authorize]
        public async Task<ActionResult<StaffUserDto>> GetCurrentStaffUser()
        {
            try
            {
                var userId = Guid.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "");
                var staffUser = await _context.StaffUsers.FindAsync(userId);

                if (staffUser == null)
                {
                    return NotFound();
                }

                return Ok(new StaffUserDto
                {
                    Id = staffUser.Id,
                    Email = staffUser.Email,
                    Nom = staffUser.Nom,
                    Prenom = staffUser.Prenom,
                    Role = staffUser.Role,
                    Department = staffUser.Department,
                    CreatedAt = staffUser.CreatedAt,
                    IsActive = staffUser.IsActive,
                    LastLoginAt = staffUser.LastLoginAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[STAFF ME] Erreur lors de la récupération des infos");
                return StatusCode(500, new { message = "Erreur serveur" });
            }
        }

        /// <summary>
        /// Lister tous les utilisateurs staff
        /// </summary>
        [HttpGet("list")]
        [Authorize]
        public async Task<ActionResult<List<StaffUserDto>>> ListStaffUsers()
        {
            try
            {
                // Optimisation: AsNoTracking pour lecture seule + projection directe
                var staffUsers = await _context.StaffUsers
                    .AsNoTracking()
                    .OrderBy(u => u.Nom)
                    .ThenBy(u => u.Prenom)
                    .Select(u => new StaffUserDto
                    {
                        Id = u.Id,
                        Email = u.Email,
                        Nom = u.Nom,
                        Prenom = u.Prenom,
                        Role = u.Role,
                        Department = u.Department,
                        CreatedAt = u.CreatedAt,
                        IsActive = u.IsActive,
                        LastLoginAt = u.LastLoginAt
                    })
                    .ToListAsync();

                return Ok(staffUsers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[STAFF LIST] Erreur lors de la récupération de la liste");
                return StatusCode(500, new { message = "Erreur serveur" });
            }
        }

        /// <summary>
        /// Désactiver un utilisateur staff
        /// </summary>
        [HttpPost("deactivate/{staffUserId}")]
        [Authorize]
        public async Task<ActionResult<StaffOperationResponse>> DeactivateStaffUser(Guid staffUserId)
        {
            try
            {

                var staffUser = await _context.StaffUsers.FindAsync(staffUserId);
                if (staffUser == null)
                {
                    return NotFound(new StaffOperationResponse
                    {
                        Success = false,
                        Message = "Utilisateur staff non trouvé"
                    });
                }

                staffUser.IsActive = false;
                staffUser.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"[STAFF DEACTIVATE] Utilisateur désactivé: {staffUser.Email}");

                return Ok(new StaffOperationResponse
                {
                    Success = true,
                    Message = "Utilisateur staff désactivé"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[STAFF DEACTIVATE] Erreur lors de la désactivation");
                return StatusCode(500, new StaffOperationResponse
                {
                    Success = false,
                    Message = "Erreur serveur"
                });
            }
        }

        /// <summary>
        /// Activer un utilisateur staff désactivé
        /// </summary>
        [HttpPost("activate/{staffUserId}")]
        [Authorize]
        public async Task<ActionResult<StaffOperationResponse>> ActivateStaffUser(Guid staffUserId)
        {
            try
            {
                var staffUser = await _context.StaffUsers.FindAsync(staffUserId);
                if (staffUser == null)
                {
                    return NotFound(new StaffOperationResponse
                    {
                        Success = false,
                        Message = "Utilisateur staff non trouvé"
                    });
                }

                staffUser.IsActive = true;
                staffUser.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"[STAFF ACTIVATE] Utilisateur activé: {staffUser.Email}");

                return Ok(new StaffOperationResponse
                {
                    Success = true,
                    Message = "Utilisateur staff activé"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[STAFF ACTIVATE] Erreur lors de l'activation");
                return StatusCode(500, new StaffOperationResponse
                {
                    Success = false,
                    Message = "Erreur serveur"
                });
            }
        }

        /// <summary>
        /// Mettre à jour les informations d'un utilisateur staff
        /// </summary>
        [HttpPut("update/{staffUserId}")]
        [Authorize]
        public async Task<ActionResult<StaffOperationResponse>> UpdateStaffUser(Guid staffUserId, UpdateStaffUserRequest request)
        {
            try
            {
                var staffUser = await _context.StaffUsers.FindAsync(staffUserId);
                if (staffUser == null)
                {
                    return NotFound(new StaffOperationResponse
                    {
                        Success = false,
                        Message = "Utilisateur staff non trouvé"
                    });
                }

                staffUser.Nom = request.Nom;
                staffUser.Prenom = request.Prenom;
                staffUser.Role = request.Role;
                staffUser.Department = request.Department;
                staffUser.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"[STAFF UPDATE] Utilisateur mis à jour: {staffUser.Email}");

                return Ok(new StaffOperationResponse
                {
                    Success = true,
                    Message = "Utilisateur staff mis à jour",
                    Data = new StaffUserDto
                    {
                        Id = staffUser.Id,
                        Email = staffUser.Email,
                        Nom = staffUser.Nom,
                        Prenom = staffUser.Prenom,
                        Role = staffUser.Role,
                        Department = staffUser.Department,
                        CreatedAt = staffUser.CreatedAt,
                        IsActive = staffUser.IsActive,
                        LastLoginAt = staffUser.LastLoginAt
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[STAFF UPDATE] Erreur lors de la mise à jour");
                return StatusCode(500, new StaffOperationResponse
                {
                    Success = false,
                    Message = "Erreur serveur"
                });
            }
        }

        /// <summary>
        /// Réinitialiser le mot de passe d'un utilisateur staff (admin uniquement)
        /// </summary>
        [HttpPost("reset-password/{staffUserId}")]
        [Authorize]
        public async Task<ActionResult<StaffOperationResponse>> ResetStaffPassword(Guid staffUserId, ResetStaffPasswordRequest request)
        {
            try
            {
                var staffUser = await _context.StaffUsers.FindAsync(staffUserId);
                if (staffUser == null)
                {
                    return NotFound(new StaffOperationResponse
                    {
                        Success = false,
                        Message = "Utilisateur staff non trouvé"
                    });
                }

                staffUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
                staffUser.RequiresPasswordChange = request.RequirePasswordChange;
                staffUser.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"[STAFF RESET PASSWORD] Mot de passe réinitialisé pour: {staffUser.Email}");

                return Ok(new StaffOperationResponse
                {
                    Success = true,
                    Message = "Mot de passe réinitialisé avec succès"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[STAFF RESET PASSWORD] Erreur lors de la réinitialisation");
                return StatusCode(500, new StaffOperationResponse
                {
                    Success = false,
                    Message = "Erreur serveur"
                });
            }
        }

        /// <summary>
        /// Initialiser le premier compte owner (ne fonctionne que si aucun staff existe)
        /// </summary>
        [HttpPost("initialize-owner")]
        [AllowAnonymous]
        public async Task<ActionResult<StaffOperationResponse>> InitializeOwner(InitializeOwnerRequest request)
        {
            try
            {
                // Vérifier qu'aucun compte staff n'existe déjà
                var staffCount = await _context.StaffUsers.CountAsync();
                if (staffCount > 0)
                {
                    _logger.LogWarning("[INITIALIZE OWNER] Tentative d'initialisation alors que des comptes staff existent déjà");
                    return BadRequest(new StaffOperationResponse
                    {
                        Success = false,
                        Message = "Des comptes staff existent déjà. Cet endpoint ne peut être utilisé qu'une seule fois."
                    });
                }

                // Créer le compte owner
                var ownerUser = new StaffUser
                {
                    Id = Guid.NewGuid(),
                    Email = request.Email,
                    Nom = request.Nom,
                    Prenom = request.Prenom,
                    Role = "SUPERADMIN",
                    Department = "Direction",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    RequiresPasswordChange = false, // Le owner n'a pas besoin de changer son mot de passe
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.StaffUsers.Add(ownerUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"[INITIALIZE OWNER] Compte owner créé: {request.Email}");

                return Ok(new StaffOperationResponse
                {
                    Success = true,
                    Message = "Compte owner créé avec succès",
                    Data = new StaffUserDto
                    {
                        Id = ownerUser.Id,
                        Email = ownerUser.Email,
                        Nom = ownerUser.Nom,
                        Prenom = ownerUser.Prenom,
                        Role = ownerUser.Role,
                        Department = ownerUser.Department,
                        CreatedAt = ownerUser.CreatedAt,
                        IsActive = ownerUser.IsActive
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[INITIALIZE OWNER] Erreur lors de l'initialisation du compte owner");
                return StatusCode(500, new StaffOperationResponse
                {
                    Success = false,
                    Message = "Erreur serveur lors de l'initialisation"
                });
            }
        }

        private string GenerateJwtToken(StaffUser staffUser)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(
                _configuration["Jwt:Key"] ?? "VotreCleSecreteTresLonguePourLaborControl2025!"));

            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, staffUser.Id.ToString()),
                new Claim(ClaimTypes.Email, staffUser.Email),
                new Claim(ClaimTypes.Role, staffUser.Role),
                new Claim("UserType", "STAFF"),  // Important: distingue staff des clients
                new Claim("StaffRole", staffUser.Role)
            };

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "LaborControl",
                audience: _configuration["Jwt:Audience"] ?? "LaborControlApp",
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}
