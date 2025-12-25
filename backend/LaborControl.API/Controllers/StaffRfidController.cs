using System.Security.Claims;
using LaborControl.API.Data;
using LaborControl.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaborControl.API.Controllers
{
    [ApiController]
    [Route("api/staff-rfid")]
    [Authorize] // Authentification requise
    public class StaffRfidController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<StaffRfidController> _logger;

        public StaffRfidController(
            ApplicationDbContext context,
            ILogger<StaffRfidController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Vérifie que l'utilisateur est un membre du staff
        /// </summary>
        private bool IsStaffUser()
        {
            var userType = User.FindFirst("UserType")?.Value;
            return userType == "STAFF";
        }

        /// <summary>
        /// GET /api/staff-rfid/chips
        /// Récupère toutes les puces RFID (pour le staff uniquement)
        /// </summary>
        [HttpGet("chips")]
        public async Task<ActionResult> GetAllChips(
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 100)
        {
            try
            {
                // Vérifier que l'utilisateur est staff
                if (!IsStaffUser())
                {
                    _logger.LogWarning("Tentative d'accès non autorisée aux puces RFID (non-staff)");
                    return Forbid();
                }

                var query = _context.RfidChips
                    .Include(c => c.Customer)
                    .Include(c => c.ControlPoint)
                        .ThenInclude(cp => cp!.Zone)
                            .ThenInclude(z => z!.Site)
                    .Include(c => c.ControlPoint)
                        .ThenInclude(cp => cp!.Asset)
                            .ThenInclude(a => a!.Zone)
                                .ThenInclude(z => z!.Site)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status))
                {
                    query = query.Where(c => c.Status == status);
                }

                var chips = await query
                    .OrderByDescending(c => c.CreatedAt)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(c => new
                    {
                        Id = c.Id,
                        ChipId = c.ChipId,
                        Uid = c.Uid,
                        Status = c.Status,
                        CreatedAt = c.CreatedAt,
                        PackagingCode = c.PackagingCode,
                        ControlPointId = c.ControlPointId,
                        CustomerName = c.Customer != null ? c.Customer.Name : null,
                        // Site name can be via Zone or Asset.Zone
                        SiteName = c.ControlPoint != null
                            ? (c.ControlPoint.Zone != null && c.ControlPoint.Zone.Site != null
                                ? c.ControlPoint.Zone.Site.Name
                                : (c.ControlPoint.Asset != null && c.ControlPoint.Asset.Zone != null && c.ControlPoint.Asset.Zone.Site != null
                                    ? c.ControlPoint.Asset.Zone.Site.Name
                                    : null))
                            : null,
                        StatusHistory = _context.RfidChipStatusHistory
                            .Where(h => h.RfidChipId == c.Id)
                            .OrderByDescending(h => h.ChangedAt)
                            .Select(h => new
                            {
                                Status = h.ToStatus,
                                ChangedAt = h.ChangedAt,
                                Reason = h.Notes
                            })
                            .ToList()
                    })
                    .ToListAsync();

                _logger.LogInformation($"✅ Staff - {chips.Count} puces récupérées");
                return Ok(chips);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur GetAllChips (Staff)");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/staff-rfid/chips/{id}
        /// Récupère une puce spécifique avec détails complets
        /// </summary>
        [HttpGet("chips/{id}")]
        public async Task<ActionResult> GetChip(Guid id)
        {
            try
            {
                // Vérifier que l'utilisateur est staff
                if (!IsStaffUser())
                {
                    _logger.LogWarning("Tentative d'accès non autorisée à la puce {ChipId} (non-staff)", id);
                    return Forbid();
                }

                var chip = await _context.RfidChips
                    .Include(c => c.Customer)
                    .Include(c => c.ControlPoint)
                        .ThenInclude(cp => cp!.Zone)
                            .ThenInclude(z => z!.Site)
                    .Include(c => c.ControlPoint)
                        .ThenInclude(cp => cp!.Asset)
                            .ThenInclude(a => a!.Zone)
                                .ThenInclude(z => z!.Site)
                    .Where(c => c.Id == id)
                    .Select(c => new
                    {
                        Id = c.Id,
                        ChipId = c.ChipId,
                        Uid = c.Uid,
                        Status = c.Status,
                        CreatedAt = c.CreatedAt,
                        PackagingCode = c.PackagingCode,
                        ControlPointId = c.ControlPointId,
                        CustomerName = c.Customer != null ? c.Customer.Name : null,
                        // Site name can be via Zone or Asset.Zone
                        SiteName = c.ControlPoint != null
                            ? (c.ControlPoint.Zone != null && c.ControlPoint.Zone.Site != null
                                ? c.ControlPoint.Zone.Site.Name
                                : (c.ControlPoint.Asset != null && c.ControlPoint.Asset.Zone != null && c.ControlPoint.Asset.Zone.Site != null
                                    ? c.ControlPoint.Asset.Zone.Site.Name
                                    : null))
                            : null,
                        StatusHistory = _context.RfidChipStatusHistory
                            .Where(h => h.RfidChipId == c.Id)
                            .OrderByDescending(h => h.ChangedAt)
                            .Select(h => new
                            {
                                Status = h.ToStatus,
                                ChangedAt = h.ChangedAt,
                                Reason = h.Notes
                            })
                            .ToList()
                    })
                    .FirstOrDefaultAsync();

                if (chip == null)
                {
                    _logger.LogWarning($"⚠️ Puce {id} non trouvée");
                    return NotFound(new { message = "Puce non trouvée" });
                }

                return Ok(chip);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"❌ Erreur GetChip {id} (Staff)");
                return StatusCode(500, new { message = ex.Message });
            }
        }

        /// <summary>
        /// GET /api/staff-rfid/stats
        /// Statistiques globales des puces RFID
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult> GetStats()
        {
            try
            {
                // Vérifier que l'utilisateur est staff
                if (!IsStaffUser())
                {
                    return Forbid();
                }

                var totalChips = await _context.RfidChips.CountAsync();
                var statsByStatus = await _context.RfidChips
                    .GroupBy(c => c.Status)
                    .Select(g => new
                    {
                        status = g.Key,
                        count = g.Count()
                    })
                    .OrderByDescending(s => s.count)
                    .ToListAsync();

                _logger.LogInformation($"✅ Staff - Statistiques récupérées: {totalChips} puces");
                return Ok(new
                {
                    total = totalChips,
                    byStatus = statsByStatus
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur GetStats (Staff)");
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}
