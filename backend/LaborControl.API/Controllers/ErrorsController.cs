using LaborControl.API.Data;
using LaborControl.API.DTOs;
using LaborControl.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaborControl.API.Controllers
{
    [ApiController]
    [Route("api/errors")]
    public class ErrorsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ErrorsController> _logger;

        public ErrorsController(ApplicationDbContext context, ILogger<ErrorsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Enregistre une erreur signalée par l'application cliente
        /// </summary>
        [HttpPost("log")]
        public async Task<ActionResult> LogError(ErrorLogRequest request)
        {
            try
            {
                var errorLog = new ErrorLog
                {
                    Id = Guid.NewGuid(),
                    Message = request.Message,
                    StackTrace = request.StackTrace,
                    PageUrl = request.PageUrl,
                    AdditionalData = request.AdditionalData,
                    UserAgent = request.UserAgent,
                    UserEmail = request.UserEmail,
                    CustomerId = request.CustomerId,
                    AppType = request.AppType,
                    Severity = request.Severity,
                    Status = "PENDING",
                    CreatedAt = DateTime.UtcNow
                };

                _context.ErrorLogs.Add(errorLog);
                await _context.SaveChangesAsync();

                // Logger aussi dans les logs applicatifs pour visibilité immédiate
                _logger.LogError(
                    $"[ERROR REPORTED] {request.Severity} - {request.Message} | Page: {request.PageUrl} | User: {request.UserEmail ?? "Anonymous"}"
                );

                return Ok(new { message = "Erreur enregistrée avec succès", errorId = errorLog.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'enregistrement d'une erreur utilisateur");
                return StatusCode(500, new { message = "Impossible d'enregistrer l'erreur" });
            }
        }

        /// <summary>
        /// Récupère toutes les erreurs (pour le futur dashboard admin)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ErrorLog>>> GetErrors(
            [FromQuery] string? status = null,
            [FromQuery] string? severity = null,
            [FromQuery] int limit = 100)
        {
            try
            {
                var query = _context.ErrorLogs
                    .AsNoTracking()
                    .OrderByDescending(e => e.CreatedAt)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(e => e.Status == status);
                }

                if (!string.IsNullOrEmpty(severity))
                {
                    query = query.Where(e => e.Severity == severity);
                }

                var errors = await query
                    .Take(limit)
                    .ToListAsync();

                return Ok(errors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des erreurs");
                return StatusCode(500, new { message = "Erreur lors de la récupération des erreurs" });
            }
        }

        /// <summary>
        /// Récupère une erreur spécifique par ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<ErrorLog>> GetError(Guid id)
        {
            try
            {
                var error = await _context.ErrorLogs
                    .AsNoTracking()
                    .FirstOrDefaultAsync(e => e.Id == id);

                if (error == null)
                {
                    return NotFound(new { message = "Erreur introuvable" });
                }

                return Ok(error);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la récupération de l'erreur {id}");
                return StatusCode(500, new { message = "Erreur lors de la récupération de l'erreur" });
            }
        }

        /// <summary>
        /// Marque une erreur comme résolue
        /// </summary>
        [HttpPatch("{id}/resolve")]
        public async Task<ActionResult> ResolveError(Guid id, [FromBody] string? resolutionNotes = null)
        {
            try
            {
                var error = await _context.ErrorLogs.FindAsync(id);

                if (error == null)
                {
                    return NotFound(new { message = "Erreur introuvable" });
                }

                error.Status = "RESOLVED";
                error.ResolvedAt = DateTime.UtcNow;
                error.ResolutionNotes = resolutionNotes;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Erreur marquée comme résolue" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Erreur lors de la résolution de l'erreur {id}");
                return StatusCode(500, new { message = "Erreur lors de la résolution" });
            }
        }

        /// <summary>
        /// Statistiques des erreurs (pour dashboard)
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult> GetErrorStats()
        {
            try
            {
                var stats = new
                {
                    total = await _context.ErrorLogs.CountAsync(),
                    pending = await _context.ErrorLogs.CountAsync(e => e.Status == "PENDING"),
                    inProgress = await _context.ErrorLogs.CountAsync(e => e.Status == "IN_PROGRESS"),
                    resolved = await _context.ErrorLogs.CountAsync(e => e.Status == "RESOLVED"),
                    critical = await _context.ErrorLogs.CountAsync(e => e.Severity == "CRITICAL" && e.Status == "PENDING"),
                    high = await _context.ErrorLogs.CountAsync(e => e.Severity == "HIGH" && e.Status == "PENDING"),
                    recentErrors = await _context.ErrorLogs
                        .Where(e => e.CreatedAt >= DateTime.UtcNow.AddDays(-7))
                        .CountAsync()
                };

                return Ok(stats);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des statistiques");
                return StatusCode(500, new { message = "Erreur lors de la récupération des statistiques" });
            }
        }
    }
}
