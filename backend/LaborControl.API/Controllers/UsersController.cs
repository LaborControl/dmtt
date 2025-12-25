using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaborControl.API.Data;
using LaborControl.API.Models;
using LaborControl.API.DTOs;
using LaborControl.API.Services;
using System.Security.Claims;

namespace LaborControl.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UsersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IUsernameGenerator _usernameGenerator;
        private readonly IEmailService _emailService;

        public UsersController(ApplicationDbContext context, IUsernameGenerator usernameGenerator, IEmailService emailService)
        {
            _context = context;
            _usernameGenerator = usernameGenerator;
            _emailService = emailService;
        }

        private Guid GetCustomerId()
        {
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;
            if (string.IsNullOrEmpty(customerIdClaim) || !Guid.TryParse(customerIdClaim, out var customerId))
            {
                throw new UnauthorizedAccessException("CustomerId introuvable ou invalide dans le token");
            }
            return customerId;
        }

        // GET: api/users/me
        [HttpGet("me")]
        public async Task<ActionResult> GetCurrentUser()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
            {
                return BadRequest(new { message = "UserId introuvable dans le token" });
            }

            var user = await _context.Users
                .Where(u => u.Id == userId && u.IsActive)
                .Select(u => new
                {
                    u.Id,
                    u.Nom,
                    u.Prenom,
                    u.Email,
                    u.Service,
                    u.Fonction,
                    u.Role
                })
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return NotFound(new { message = "Utilisateur non trouvé" });
            }

            return Ok(user);
        }

        // GET: api/users
        [HttpGet]
        public async Task<ActionResult<IEnumerable<User>>> GetUsers()
        {
            var customerId = GetCustomerId();

            var users = await _context.Users
                .Include(u => u.Site)
                .Include(u => u.Team)
                    .ThenInclude(t => t.ParentTeam)
                .Include(u => u.Industry)
                .Include(u => u.Supervisor)
                .Where(u => u.CustomerId == customerId && u.IsActive)
                .Select(u => new
                {
                    u.Id,
                    u.Nom,
                    u.Prenom,
                    u.Email,
                    u.Tel,
                    u.Service,
                    u.Fonction,
                    u.Niveau,
                    u.Role,
                    u.IsActive,
                    SiteName = u.Site != null ? u.Site.Name : null,
                    TeamName = u.Team != null ? u.Team.Name : null,
                    ServiceId = u.Team != null && u.Team.ParentTeam != null ? u.Team.ParentTeamId : null,
                    ServiceName = u.Team != null && u.Team.ParentTeam != null ? u.Team.ParentTeam.Name : null,
                    IndustryName = u.Industry != null ? u.Industry.Name : null,
                    SupervisorName = u.Supervisor != null ? u.Supervisor.Prenom + " " + u.Supervisor.Nom : null
                })
                .ToListAsync();

            return Ok(users);
        }

        // GET: api/users/technicians
        [HttpGet("technicians")]
        public async Task<ActionResult> GetTechnicians()
        {
            var customerId = GetCustomerId();

            // Retourne uniquement les techniciens (exclut admins et managers)
            var technicians = await _context.Users
                .Where(u => u.CustomerId == customerId &&
                           u.IsActive &&
                           u.Role.ToUpper() != "ADMIN" &&
                           u.Role.ToUpper() != "MANAGER")
                .Select(u => new
                {
                    u.Id,
                    u.Nom,
                    u.Prenom,
                    u.Email,
                    u.Role
                })
                .OrderBy(u => u.Nom)
                .ThenBy(u => u.Prenom)
                .ToListAsync();

            return Ok(technicians);
        }

        // GET: api/users/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<User>> GetUser(Guid id)
        {
            var customerId = GetCustomerId();

            var user = await _context.Users
                .Include(u => u.Team)
                .FirstOrDefaultAsync(u => u.Id == id && u.CustomerId == customerId);

            if (user == null)
                return NotFound();

            return Ok(new
            {
                user.Id,
                user.Nom,
                user.Prenom,
                user.Email,
                user.Username,
                Phone = user.Tel,
                user.Role,
                user.JobTitle,
                user.IsActive,
                user.CreatedAt,
                user.TeamId,
                TeamName = user.Team?.Name
            });
        }

        // POST: api/users
        [HttpPost]
        public async Task<ActionResult<User>> CreateUser([FromBody] CreateUserRequest request)
        {
            try
            {
                var customerId = GetCustomerId();

                // Valider que le CustomerId existe et est actif
                var customerExists = await _context.Customers
                    .AnyAsync(c => c.Id == customerId && c.IsActive);

                if (!customerExists)
                {
                    return BadRequest(new { message = "Le client n'existe pas ou est inactif" });
                }

                // Valider que le SectorId est fourni et existe (OBLIGATOIRE)
                if (request.SectorId == Guid.Empty)
                {
                    return BadRequest(new { message = "Le secteur d'activité est obligatoire" });
                }

                var sectorExists = await _context.Sectors
                    .AnyAsync(s => s.Id == request.SectorId && s.IsActive);

                if (!sectorExists)
                {
                    return BadRequest(new { message = "Le secteur sélectionné n'existe pas ou est inactif" });
                }

                // Valider que l'IndustryId est fourni et existe (OBLIGATOIRE)
                if (request.IndustryId == Guid.Empty)
                {
                    return BadRequest(new { message = "Le métier est obligatoire" });
                }

                var industryExists = await _context.Industries
                    .AnyAsync(i => i.Id == request.IndustryId && i.IsActive);

                if (!industryExists)
                {
                    return BadRequest(new { message = "Le métier sélectionné n'existe pas ou est inactif" });
                }

                // Valider que le SiteId existe et est actif (si fourni)
                if (request.SiteId.HasValue)
                {
                    var siteExists = await _context.Sites
                        .AnyAsync(s => s.Id == request.SiteId.Value && s.CustomerId == customerId && s.IsActive);

                    if (!siteExists)
                    {
                        return BadRequest(new { message = "Le site sélectionné n'existe pas ou est inactif" });
                    }
                }

                // Valider que le TeamId existe et est actif (si fourni)
                if (request.TeamId.HasValue)
                {
                    var teamExists = await _context.Teams
                        .AnyAsync(t => t.Id == request.TeamId.Value && t.CustomerId == customerId && t.IsActive);

                    if (!teamExists)
                    {
                        return BadRequest(new { message = "L'équipe sélectionnée n'existe pas ou est inactive" });
                    }
                }

                // Générer le username automatiquement (format: PrénomNNN)
                var generatedUsername = await _usernameGenerator.GenerateUniqueUsernameAsync(
                    request.Prenom,
                    request.Nom,
                    customerId);

                // Générer un code PIN à 4 chiffres pour la première connexion
                var random = new Random();
                var setupPin = random.Next(1000, 10000).ToString(); // Génère un nombre entre 1000 et 9999

                // Créer l'utilisateur
                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Prenom = request.Prenom,
                    Nom = request.Nom,
                    Email = request.Email,
                    Username = generatedUsername,
                    Tel = request.Phone ?? string.Empty,
                    Role = request.Role,
                    SectorId = request.SectorId,
                    IndustryId = request.IndustryId,
                    SiteId = request.SiteId,
                    TeamId = request.TeamId,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                    RequiresPasswordChange = true,
                    SetupPin = setupPin,
                    SetupPinExpiresAt = DateTime.UtcNow.AddHours(72), // PIN expire après 72 heures
                    CustomerId = customerId,
                    IsActive = true,
                    IsAccountOwner = false,  // Employé, pas le propriétaire du compte
                    CreatedAt = DateTime.UtcNow
                };

                _context.Users.Add(user);

                // Créer les UserQualifications
                if (request.Qualifications != null)
                {
                    foreach (var qualReq in request.Qualifications)
                    {
                        var qualification = await _context.Qualifications.FindAsync(qualReq.QualificationId);
                        if (qualification == null) continue;

                        // Convertir la date en UTC si nécessaire
                        var obtainedDateUtc = qualReq.ObtainedDate.Kind == DateTimeKind.Utc
                            ? qualReq.ObtainedDate
                            : DateTime.SpecifyKind(qualReq.ObtainedDate, DateTimeKind.Utc);

                        var userQual = new UserQualification
                        {
                            Id = Guid.NewGuid(),
                            UserId = user.Id,
                            QualificationId = qualReq.QualificationId,
                            ObtainedDate = obtainedDateUtc,
                            ExpirationDate = qualification.RequiresRenewal && qualification.ValidityPeriodMonths.HasValue
                                ? obtainedDateUtc.AddMonths(qualification.ValidityPeriodMonths.Value)
                                : null,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.UserQualifications.Add(userQual);
                    }
                }

                await _context.SaveChangesAsync();

                // Envoyer l'email de bienvenue si une adresse email est fournie
                if (!string.IsNullOrWhiteSpace(user.Email))
                {
                    try
                    {
                        var emailSent = await _emailService.SendNewEmployeeWelcomeEmailAsync(
                            user.Email,
                            user.Prenom,
                            user.Nom,
                            user.Username ?? "",
                            setupPin,
                            user.SetupPinExpiresAt ?? DateTime.UtcNow.AddHours(72)
                        );

                        if (emailSent)
                        {
                            Console.WriteLine($"[USER CREATE] Email de bienvenue envoyé à {user.Email}");
                        }
                        else
                        {
                            Console.WriteLine($"[USER CREATE] Échec de l'envoi de l'email de bienvenue à {user.Email}");
                        }
                    }
                    catch (Exception emailEx)
                    {
                        // Ne pas bloquer la création de l'utilisateur si l'email échoue
                        Console.WriteLine($"[USER CREATE] Erreur lors de l'envoi de l'email de bienvenue: {emailEx.Message}");
                    }
                }

                return CreatedAtAction(nameof(GetUser), new { id = user.Id }, new
                {
                    userId = user.Id,
                    username = user.Username,
                    setupPin = setupPin,
                    pinExpiresAt = user.SetupPinExpiresAt
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de la création de l'utilisateur", error = ex.Message });
            }
        }

        // PUT: api/users/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
        {
            var customerId = GetCustomerId();

            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.CustomerId == customerId);

            if (existingUser == null)
                return NotFound();

            existingUser.Nom = dto.Nom;
            existingUser.Prenom = dto.Prenom;
            existingUser.Email = dto.Email;
            existingUser.Tel = dto.Phone ?? "";
            existingUser.Role = dto.Role;
            existingUser.JobTitle = dto.JobTitle;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        public class UpdateUserDto
        {
            public string Prenom { get; set; } = "";
            public string Nom { get; set; } = "";
            public string? Email { get; set; }
            public string? Phone { get; set; }
            public string Role { get; set; } = "";
            public string JobTitle { get; set; } = "";
        }

        // DELETE: api/users/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteUser(Guid id)
        {
            var customerId = GetCustomerId();

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == id && u.CustomerId == customerId);

            if (user == null)
                return NotFound();

            user.IsActive = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/users/migrate-usernames
        // ⚠️ ENDPOINT TEMPORAIRE - Migration one-time pour générer les usernames manquants
        [AllowAnonymous]
        [HttpPost("migrate-usernames")]
        public async Task<ActionResult> MigrateUsernames()
        {
            try
            {
                // Récupérer tous les users sans username (null ou vide)
                var usersWithoutUsername = await _context.Users
                    .Where(u => string.IsNullOrEmpty(u.Username))
                    .ToListAsync();

                if (usersWithoutUsername.Count == 0)
                {
                    return Ok(new
                    {
                        message = "Aucun utilisateur sans username trouvé",
                        totalProcessed = 0
                    });
                }

                var migrationResults = new List<object>();
                var successCount = 0;
                var errorCount = 0;

                foreach (var user in usersWithoutUsername)
                {
                    try
                    {
                        // Générer le username
                        var generatedUsername = await _usernameGenerator.GenerateUniqueUsernameAsync(
                            user.Prenom,
                            user.Nom,
                            user.CustomerId);

                        user.Username = generatedUsername;

                        migrationResults.Add(new
                        {
                            userId = user.Id,
                            nom = user.Nom,
                            prenom = user.Prenom,
                            email = user.Email,
                            generatedUsername = generatedUsername,
                            status = "success"
                        });

                        successCount++;
                    }
                    catch (Exception ex)
                    {
                        migrationResults.Add(new
                        {
                            userId = user.Id,
                            nom = user.Nom,
                            prenom = user.Prenom,
                            email = user.Email,
                            status = "error",
                            error = ex.Message
                        });

                        errorCount++;
                    }
                }

                // Sauvegarder toutes les modifications
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    message = $"Migration terminée: {successCount} réussies, {errorCount} erreurs",
                    totalProcessed = usersWithoutUsername.Count,
                    successCount = successCount,
                    errorCount = errorCount,
                    details = migrationResults
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Erreur lors de la migration des usernames",
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Réinitialise le mot de passe d'un utilisateur et génère un nouveau code PIN
        /// </summary>
        [HttpPost("{id}/reset-password")]
        public async Task<ActionResult> ResetUserPassword(Guid id)
        {
            try
            {
                var customerId = GetCustomerId();

                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == id && u.CustomerId == customerId);

                if (user == null)
                {
                    return NotFound(new { message = "Utilisateur introuvable" });
                }

                // Générer un nouveau code PIN à 4 chiffres
                var random = new Random();
                var setupPin = random.Next(1000, 10000).ToString();

                // Réinitialiser le mot de passe avec le PIN
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(setupPin);
                user.SetupPin = setupPin;
                user.SetupPinExpiresAt = DateTime.UtcNow.AddHours(72); // PIN expire après 72 heures
                user.RequiresPasswordChange = true;

                await _context.SaveChangesAsync();

                // Envoyer un email si l'utilisateur en a un
                if (!string.IsNullOrEmpty(user.Email))
                {
                    try
                    {
                        await _emailService.SendNewEmployeeWelcomeEmailAsync(
                            user.Email,
                            user.Prenom,
                            user.Nom,
                            user.Username ?? "",
                            setupPin,
                            user.SetupPinExpiresAt ?? DateTime.UtcNow.AddHours(72)
                        );
                    }
                    catch (Exception emailEx)
                    {
                        // Log l'erreur mais ne pas bloquer la réinitialisation
                        Console.WriteLine($"Erreur lors de l'envoi de l'email: {emailEx.Message}");
                    }
                }

                return Ok(new
                {
                    message = "Mot de passe réinitialisé avec succès",
                    username = user.Username,
                    setupPin = setupPin,
                    pinExpiresAt = user.SetupPinExpiresAt,
                    emailSent = !string.IsNullOrEmpty(user.Email)
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new
                {
                    message = "Erreur lors de la réinitialisation du mot de passe",
                    error = ex.Message
                });
            }
        }
    }
}
