using LaborControl.API.Data;
using LaborControl.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaborControl.API.Controllers
{
    /// <summary>
    /// Endpoint de diagnostic pour vérifier l'état de l'application
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class DiagnosticsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DiagnosticsController> _logger;
        private readonly IConfiguration _configuration;

        public DiagnosticsController(
            ApplicationDbContext context,
            ILogger<DiagnosticsController> logger,
            IConfiguration configuration)
        {
            _context = context;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Vérifie l'état de la base de données et des migrations
        /// </summary>
        [HttpGet("database-status")]
        public async Task<ActionResult> GetDatabaseStatus()
        {
            try
            {
                var status = new
                {
                    timestamp = DateTime.UtcNow,
                    database = new
                    {
                        canConnect = false,
                        connectionString = "hidden",
                        pendingMigrations = new List<string>(),
                        appliedMigrations = new List<string>(),
                        lastMigration = "",
                    },
                    tables = new
                    {
                        passwordResetTokensExists = false,
                        staffUsersHasPasswordResetToken = false,
                    },
                    configuration = new
                    {
                        hasClientAppUrl = !string.IsNullOrEmpty(_configuration["AppSettings:ClientAppUrl"]),
                        hasStaffAppUrl = !string.IsNullOrEmpty(_configuration["AppSettings:StaffAppUrl"]),
                        clientAppUrl = _configuration["AppSettings:ClientAppUrl"],
                        staffAppUrl = _configuration["AppSettings:StaffAppUrl"],
                        envClientAppUrl = Environment.GetEnvironmentVariable("AppSettings__ClientAppUrl"),
                        envStaffAppUrl = Environment.GetEnvironmentVariable("AppSettings__StaffAppUrl"),
                    }
                };

                // Test de connexion
                var canConnect = await _context.Database.CanConnectAsync();
                status = status with
                {
                    database = status.database with { canConnect = canConnect }
                };

                if (!canConnect)
                {
                    return Ok(status);
                }

                // Migrations pendantes
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
                var appliedMigrations = await _context.Database.GetAppliedMigrationsAsync();

                status = status with
                {
                    database = status.database with
                    {
                        pendingMigrations = pendingMigrations.ToList(),
                        appliedMigrations = appliedMigrations.ToList(),
                        lastMigration = appliedMigrations.LastOrDefault() ?? "none"
                    }
                };

                // Vérifier si la table PasswordResetTokens existe
                try
                {
                    await _context.PasswordResetTokens.CountAsync();
                    status = status with
                    {
                        tables = status.tables with { passwordResetTokensExists = true }
                    };
                }
                catch
                {
                    // Table n'existe pas
                }

                // Vérifier si StaffUsers a les colonnes PasswordResetToken
                try
                {
                    var hasColumn = await _context.Database.ExecuteSqlRawAsync(
                        "SELECT 1 FROM information_schema.columns WHERE table_name = 'StaffUsers' AND column_name = 'PasswordResetToken'"
                    );
                    status = status with
                    {
                        tables = status.tables with { staffUsersHasPasswordResetToken = hasColumn > 0 }
                    };
                }
                catch
                {
                    // Erreur lors de la vérification
                }

                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du diagnostic de la base de données");
                return StatusCode(500, new
                {
                    error = true,
                    message = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        /// <summary>
        /// Endpoint de santé basique
        /// </summary>
        [HttpGet("health")]
        public ActionResult GetHealth()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow,
                environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "unknown"
            });
        }

        /// <summary>
        /// Force l'application de toutes les migrations en attente
        /// ATTENTION: Utiliser avec précaution en production!
        /// </summary>
        [HttpPost("apply-migrations")]
        public async Task<ActionResult> ApplyMigrations([FromQuery] string confirmKey)
        {
            // Simple protection - ne pas exposer sans authentification en production réelle
            if (confirmKey != "APPLY-MIGRATIONS-LABOR-CONTROL")
            {
                return Unauthorized(new { error = "Invalid confirmation key" });
            }

            try
            {
                _logger.LogWarning("[MIGRATIONS] Début de l'application manuelle des migrations");

                var pendingMigrations = (await _context.Database.GetPendingMigrationsAsync()).ToList();

                if (!pendingMigrations.Any())
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Aucune migration en attente",
                        appliedCount = 0
                    });
                }

                _logger.LogWarning($"[MIGRATIONS] {pendingMigrations.Count} migrations en attente: {string.Join(", ", pendingMigrations)}");

                // Appliquer les migrations
                await _context.Database.MigrateAsync();

                _logger.LogWarning("[MIGRATIONS] Migrations appliquées avec succès");

                var appliedMigrations = (await _context.Database.GetAppliedMigrationsAsync()).ToList();

                return Ok(new
                {
                    success = true,
                    message = "Migrations appliquées avec succès",
                    appliedCount = pendingMigrations.Count,
                    previouslyPending = pendingMigrations,
                    totalApplied = appliedMigrations.Count,
                    lastMigration = appliedMigrations.LastOrDefault()
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[MIGRATIONS] Erreur lors de l'application des migrations");
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        /// <summary>
        /// Créer le compte RQ pour DMTT
        /// </summary>
        [HttpPost("create-dmtt-rq")]
        public async Task<ActionResult> CreateDmttRQ([FromQuery] string confirmKey)
        {
            if (confirmKey != "DMTT-RQ-2026")
            {
                return Unauthorized(new { error = "Invalid confirmation key" });
            }

            try
            {
                var email = "codjo@labor-control.fr";

                // Vérifier si l'utilisateur existe déjà
                var existingUser = await _context.StaffUsers.FirstOrDefaultAsync(u => u.Email == email);
                if (existingUser != null)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Compte RQ existe déjà",
                        email = email,
                        userId = existingUser.Id
                    });
                }

                // Créer le compte RQ
                var rqUser = new StaffUser
                {
                    Id = Guid.NewGuid(),
                    Email = email,
                    Nom = "Codjo",
                    Prenom = "Responsable Qualité",
                    Role = "SUPERADMIN",
                    Department = "Qualité",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("LcDmtt2026RQ"),
                    RequiresPasswordChange = true,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true
                };

                _context.StaffUsers.Add(rqUser);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"[DMTT] Compte RQ créé: {email}");

                return Ok(new
                {
                    success = true,
                    message = "Compte RQ DMTT créé avec succès",
                    email = email,
                    password = "LcDmtt2026RQ",
                    userId = rqUser.Id,
                    note = "Le mot de passe devra être changé à la première connexion"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DMTT] Erreur lors de la création du compte RQ");
                return StatusCode(500, new
                {
                    success = false,
                    error = ex.Message
                });
            }
        }

        /// <summary>
        /// Liste les comptes staff existants
        /// </summary>
        [HttpGet("staff-users")]
        public async Task<ActionResult> GetStaffUsers([FromQuery] string confirmKey)
        {
            if (confirmKey != "DMTT-RQ-2026")
            {
                return Unauthorized(new { error = "Invalid confirmation key" });
            }

            var staffUsers = await _context.StaffUsers
                .Select(u => new { u.Id, u.Email, u.Nom, u.Prenom, u.Role, u.IsActive, u.CreatedAt })
                .ToListAsync();

            return Ok(new
            {
                count = staffUsers.Count,
                users = staffUsers
            });
        }
    }
}
