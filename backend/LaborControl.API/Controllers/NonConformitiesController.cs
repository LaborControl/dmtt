using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaborControl.API.Data;
using LaborControl.API.Models;
using LaborControl.API.DTOs;

namespace LaborControl.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class NonConformitiesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NonConformitiesController> _logger;

        public NonConformitiesController(ApplicationDbContext context, ILogger<NonConformitiesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private Guid GetCustomerId()
        {
            var customerIdClaim = HttpContext.User.FindFirst("CustomerId")?.Value;
            return Guid.TryParse(customerIdClaim, out var customerId) ? customerId : Guid.Empty;
        }

        private Guid GetUserId()
        {
            var userIdClaim = HttpContext.User.FindFirst("UserId")?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }

        // GET: api/nonconformities
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NonConformityDto>>> GetNonConformities(
            [FromQuery] string? status = null,
            [FromQuery] string? type = null,
            [FromQuery] string? severity = null,
            [FromQuery] Guid? weldId = null,
            [FromQuery] Guid? materialId = null,
            [FromQuery] bool? overdue = null)
        {
            try
            {
                var customerId = GetCustomerId();

                var query = _context.NonConformities
                    .Where(nc => nc.CustomerId == customerId && nc.IsActive);

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(nc => nc.Status == status);

                if (!string.IsNullOrEmpty(type))
                    query = query.Where(nc => nc.Type == type);

                if (!string.IsNullOrEmpty(severity))
                    query = query.Where(nc => nc.Severity == severity);

                if (weldId.HasValue)
                    query = query.Where(nc => nc.WeldId == weldId.Value);

                if (materialId.HasValue)
                    query = query.Where(nc => nc.MaterialId == materialId.Value);

                if (overdue.HasValue && overdue.Value)
                    query = query.Where(nc => nc.DueDate.HasValue && nc.DueDate.Value < DateTime.UtcNow && nc.Status != "CLOSED");

                var nonConformities = await query
                    .AsNoTracking()
                    .OrderByDescending(nc => nc.Severity == "CRITICAL")
                    .ThenByDescending(nc => nc.Severity == "MAJOR")
                    .ThenByDescending(nc => nc.CreatedAt)
                    .Select(nc => new NonConformityDto
                    {
                        Id = nc.Id,
                        Reference = nc.Reference,
                        Title = nc.Title,
                        Type = nc.Type,
                        Severity = nc.Severity,
                        WeldId = nc.WeldId,
                        WeldReference = nc.Weld != null ? nc.Weld.Reference : null,
                        MaterialId = nc.MaterialId,
                        MaterialReference = nc.Material != null ? nc.Material.Reference : null,
                        AssetId = nc.AssetId,
                        AssetName = nc.Asset != null ? nc.Asset.Name : null,
                        Status = nc.Status,
                        CreatedById = nc.CreatedById,
                        CreatedByName = nc.CreatedBy != null
                            ? nc.CreatedBy.Prenom + " " + nc.CreatedBy.Nom
                            : "",
                        DetectionDate = nc.DetectionDate,
                        DueDate = nc.DueDate,
                        ActionResponsibleId = nc.ActionResponsibleId,
                        ActionResponsibleName = nc.ActionResponsible != null
                            ? nc.ActionResponsible.Prenom + " " + nc.ActionResponsible.Nom
                            : null,
                        RequiresRecontrol = nc.RequiresRecontrol,
                        CreatedAt = nc.CreatedAt
                    })
                    .ToListAsync();

                return Ok(nonConformities);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des FNC");
                return StatusCode(500, "Erreur serveur");
            }
        }

        // GET: api/nonconformities/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<NonConformityDetailDto>> GetNonConformity(Guid id)
        {
            try
            {
                var customerId = GetCustomerId();

                var nc = await _context.NonConformities
                    .Include(n => n.Weld)
                    .Include(n => n.Material)
                    .Include(n => n.Asset)
                    .Include(n => n.CreatedBy)
                    .Include(n => n.ActionResponsible)
                    .Include(n => n.ClosedBy)
                    .FirstOrDefaultAsync(n => n.Id == id && n.CustomerId == customerId);

                if (nc == null)
                    return NotFound("FNC introuvable");

                var dto = new NonConformityDetailDto
                {
                    Id = nc.Id,
                    Reference = nc.Reference,
                    Title = nc.Title,
                    Type = nc.Type,
                    Severity = nc.Severity,
                    WeldId = nc.WeldId,
                    WeldReference = nc.Weld?.Reference,
                    MaterialId = nc.MaterialId,
                    MaterialReference = nc.Material?.Reference,
                    AssetId = nc.AssetId,
                    AssetName = nc.Asset?.Name,
                    Description = nc.Description,
                    RootCause = nc.RootCause,
                    Status = nc.Status,
                    CreatedById = nc.CreatedById,
                    CreatedByName = nc.CreatedBy != null
                        ? $"{nc.CreatedBy.Prenom} {nc.CreatedBy.Nom}"
                        : "",
                    DetectionDate = nc.DetectionDate,
                    CorrectiveAction = nc.CorrectiveAction,
                    PreventiveAction = nc.PreventiveAction,
                    ActionResponsibleId = nc.ActionResponsibleId,
                    ActionResponsibleName = nc.ActionResponsible != null
                        ? $"{nc.ActionResponsible.Prenom} {nc.ActionResponsible.Nom}"
                        : null,
                    DueDate = nc.DueDate,
                    ResolutionDate = nc.ResolutionDate,
                    ClosedById = nc.ClosedById,
                    ClosedByName = nc.ClosedBy != null
                        ? $"{nc.ClosedBy.Prenom} {nc.ClosedBy.Nom}"
                        : null,
                    ClosedDate = nc.ClosedDate,
                    ClosureComments = nc.ClosureComments,
                    Attachments = nc.Attachments,
                    ActionHistory = nc.ActionHistory,
                    EstimatedCost = nc.EstimatedCost,
                    ScheduleImpactDays = nc.ScheduleImpactDays,
                    RequiresRecontrol = nc.RequiresRecontrol,
                    VerificationControlId = nc.VerificationControlId,
                    AIRecommendation = nc.AIRecommendation,
                    CreatedAt = nc.CreatedAt,
                    UpdatedAt = nc.UpdatedAt
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la FNC {NCId}", id);
                return StatusCode(500, "Erreur serveur");
            }
        }

        // POST: api/nonconformities
        [HttpPost]
        public async Task<ActionResult<NonConformityDto>> CreateNonConformity([FromBody] CreateNonConformityRequest request)
        {
            try
            {
                var customerId = GetCustomerId();
                var userId = GetUserId();

                var nc = new NonConformity
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    Reference = $"FNC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
                    Title = request.Title,
                    Type = request.Type,
                    Severity = request.Severity,
                    WeldId = request.WeldId,
                    MaterialId = request.MaterialId,
                    AssetId = request.AssetId,
                    Description = request.Description,
                    Status = "OPEN",
                    CreatedById = userId,
                    DetectionDate = DateTime.UtcNow,
                    DueDate = request.DueDate,
                    ActionResponsibleId = request.ActionResponsibleId,
                    RequiresRecontrol = request.RequiresRecontrol,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.NonConformities.Add(nc);

                // Si liée à une soudure, mettre à jour son statut
                if (request.WeldId.HasValue)
                {
                    var weld = await _context.Welds.FindAsync(request.WeldId.Value);
                    if (weld != null)
                    {
                        weld.Status = "NON_CONFORM";
                        weld.UpdatedAt = DateTime.UtcNow;
                    }
                }

                // Si liée à un matériau, le bloquer
                if (request.MaterialId.HasValue)
                {
                    var material = await _context.Materials.FindAsync(request.MaterialId.Value);
                    if (material != null)
                    {
                        material.IsBlocked = true;
                        material.BlockReason = $"FNC {nc.Reference}";
                        material.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();

                var user = await _context.Users.FindAsync(userId);

                var dto = new NonConformityDto
                {
                    Id = nc.Id,
                    Reference = nc.Reference,
                    Title = nc.Title,
                    Type = nc.Type,
                    Severity = nc.Severity,
                    Status = nc.Status,
                    CreatedById = nc.CreatedById,
                    CreatedByName = user != null ? $"{user.Prenom} {user.Nom}" : "",
                    DetectionDate = nc.DetectionDate,
                    CreatedAt = nc.CreatedAt
                };

                return CreatedAtAction(nameof(GetNonConformity), new { id = nc.Id }, dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la FNC");
                return StatusCode(500, "Erreur serveur");
            }
        }

        // PUT: api/nonconformities/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateNonConformity(Guid id, [FromBody] UpdateNonConformityRequest request)
        {
            try
            {
                var customerId = GetCustomerId();

                var nc = await _context.NonConformities.FirstOrDefaultAsync(n =>
                    n.Id == id && n.CustomerId == customerId);

                if (nc == null)
                    return NotFound("FNC introuvable");

                if (nc.Status == "CLOSED")
                    return BadRequest("Impossible de modifier une FNC clôturée");

                nc.Title = request.Title;
                nc.Type = request.Type;
                nc.Severity = request.Severity;
                nc.Description = request.Description;
                nc.RootCause = request.RootCause;
                nc.CorrectiveAction = request.CorrectiveAction;
                nc.PreventiveAction = request.PreventiveAction;
                nc.DueDate = request.DueDate;
                nc.ActionResponsibleId = request.ActionResponsibleId;
                nc.EstimatedCost = request.EstimatedCost;
                nc.ScheduleImpactDays = request.ScheduleImpactDays;
                nc.RequiresRecontrol = request.RequiresRecontrol;
                nc.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la FNC {NCId}", id);
                return StatusCode(500, "Erreur serveur");
            }
        }

        // POST: api/nonconformities/{id}/status
        [HttpPost("{id}/status")]
        public async Task<IActionResult> ChangeStatus(Guid id, [FromBody] NonConformityStatusChangeRequest request)
        {
            try
            {
                var customerId = GetCustomerId();
                var userId = GetUserId();

                var nc = await _context.NonConformities.FirstOrDefaultAsync(n =>
                    n.Id == id && n.CustomerId == customerId);

                if (nc == null)
                    return NotFound("FNC introuvable");

                // Valider la transition de statut
                var validTransitions = new Dictionary<string, string[]>
                {
                    ["OPEN"] = new[] { "ANALYSIS", "CANCELLED" },
                    ["ANALYSIS"] = new[] { "PENDING_ACTION", "OPEN" },
                    ["PENDING_ACTION"] = new[] { "ACTION_IN_PROGRESS", "ANALYSIS" },
                    ["ACTION_IN_PROGRESS"] = new[] { "PENDING_VERIFICATION", "PENDING_ACTION" },
                    ["PENDING_VERIFICATION"] = new[] { "CLOSED", "ACTION_IN_PROGRESS" }
                };

                if (!validTransitions.ContainsKey(nc.Status) ||
                    !validTransitions[nc.Status].Contains(request.NewStatus))
                {
                    return BadRequest($"Transition de {nc.Status} vers {request.NewStatus} non autorisée");
                }

                var previousStatus = nc.Status;
                nc.Status = request.NewStatus;

                // Ajouter à l'historique
                var historyEntry = new
                {
                    Date = DateTime.UtcNow,
                    UserId = userId,
                    FromStatus = previousStatus,
                    ToStatus = request.NewStatus,
                    Comments = request.Comments,
                    ActionDetails = request.ActionDetails
                };

                // Mettre à jour l'historique (simplifié - en production, parser et ajouter au JSON)
                nc.ActionHistory = System.Text.Json.JsonSerializer.Serialize(new[] { historyEntry });
                nc.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new {
                    Message = $"Statut changé de {previousStatus} à {request.NewStatus}",
                    NewStatus = nc.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du changement de statut de la FNC {NCId}", id);
                return StatusCode(500, "Erreur serveur");
            }
        }

        // POST: api/nonconformities/{id}/close
        [HttpPost("{id}/close")]
        public async Task<IActionResult> CloseNonConformity(Guid id, [FromBody] CloseNonConformityRequest request)
        {
            try
            {
                var customerId = GetCustomerId();
                var userId = GetUserId();

                var nc = await _context.NonConformities.FirstOrDefaultAsync(n =>
                    n.Id == id && n.CustomerId == customerId);

                if (nc == null)
                    return NotFound("FNC introuvable");

                // Vérifier les droits RQ
                var user = await _context.Users.FindAsync(userId);
                if (user == null || !user.CanValidateQuality)
                    return Forbid("Seul le RQ peut clôturer une FNC");

                if (nc.Status != "PENDING_VERIFICATION")
                    return BadRequest("La FNC doit être en attente de vérification pour être clôturée");

                // Si re-contrôle requis, vérifier qu'il a été fait
                if (nc.RequiresRecontrol && !request.VerificationControlId.HasValue)
                    return BadRequest("Un contrôle de vérification est requis");

                nc.Status = "CLOSED";
                nc.ClosedById = userId;
                nc.ClosedDate = DateTime.UtcNow;
                nc.ClosureComments = request.ClosureComments;
                nc.VerificationControlId = request.VerificationControlId;
                nc.ResolutionDate = DateTime.UtcNow;
                nc.UpdatedAt = DateTime.UtcNow;

                // Si liée à une soudure, vérifier si elle peut être remise en service
                if (nc.WeldId.HasValue)
                {
                    var weld = await _context.Welds.FindAsync(nc.WeldId.Value);
                    if (weld != null)
                    {
                        // Vérifier s'il y a d'autres FNC ouvertes sur cette soudure
                        var hasOtherOpenNC = await _context.NonConformities
                            .AnyAsync(n => n.WeldId == nc.WeldId && n.Id != nc.Id && n.IsActive && n.Status != "CLOSED");

                        if (!hasOtherOpenNC)
                        {
                            weld.Status = "COMPLETED";
                            weld.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }

                // Si liée à un matériau, débloquer si pas d'autres FNC
                if (nc.MaterialId.HasValue)
                {
                    var material = await _context.Materials.FindAsync(nc.MaterialId.Value);
                    if (material != null)
                    {
                        var hasOtherOpenNC = await _context.NonConformities
                            .AnyAsync(n => n.MaterialId == nc.MaterialId && n.Id != nc.Id && n.IsActive && n.Status != "CLOSED");

                        if (!hasOtherOpenNC)
                        {
                            material.IsBlocked = false;
                            material.BlockReason = null;
                            material.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }

                await _context.SaveChangesAsync();

                return Ok(new {
                    Message = "FNC clôturée avec succès",
                    Reference = nc.Reference
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la clôture de la FNC {NCId}", id);
                return StatusCode(500, "Erreur serveur");
            }
        }

        // GET: api/nonconformities/dashboard
        [HttpGet("dashboard")]
        public async Task<ActionResult<NonConformityDashboardDto>> GetDashboard()
        {
            try
            {
                var customerId = GetCustomerId();

                var ncs = await _context.NonConformities
                    .Where(nc => nc.CustomerId == customerId && nc.IsActive)
                    .ToListAsync();

                var dashboard = new NonConformityDashboardDto
                {
                    TotalNonConformities = ncs.Count,
                    OpenNonConformities = ncs.Count(nc => nc.Status == "OPEN" || nc.Status == "ANALYSIS"),
                    InProgressNonConformities = ncs.Count(nc =>
                        nc.Status == "PENDING_ACTION" ||
                        nc.Status == "ACTION_IN_PROGRESS" ||
                        nc.Status == "PENDING_VERIFICATION"),
                    ClosedNonConformities = ncs.Count(nc => nc.Status == "CLOSED"),
                    OverdueNonConformities = ncs.Count(nc =>
                        nc.DueDate.HasValue &&
                        nc.DueDate.Value < DateTime.UtcNow &&
                        nc.Status != "CLOSED"),
                    CriticalNonConformities = ncs.Count(nc => nc.Severity == "CRITICAL" && nc.Status != "CLOSED"),
                    MajorNonConformities = ncs.Count(nc => nc.Severity == "MAJOR" && nc.Status != "CLOSED"),
                    MinorNonConformities = ncs.Count(nc => nc.Severity == "MINOR" && nc.Status != "CLOSED"),
                    TotalEstimatedCost = ncs.Sum(nc => nc.EstimatedCost ?? 0),
                    TotalScheduleImpactDays = ncs.Sum(nc => nc.ScheduleImpactDays ?? 0),
                    TypeBreakdown = ncs
                        .GroupBy(nc => nc.Type)
                        .Select(g => new NonConformityTypeCount { Type = g.Key, Count = g.Count() })
                        .ToList(),
                    StatusBreakdown = ncs
                        .GroupBy(nc => nc.Status)
                        .Select(g => new NonConformityStatusCount { Status = g.Key, Count = g.Count() })
                        .ToList()
                };

                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du dashboard FNC");
                return StatusCode(500, "Erreur serveur");
            }
        }

        // DELETE: api/nonconformities/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNonConformity(Guid id)
        {
            try
            {
                var customerId = GetCustomerId();

                var nc = await _context.NonConformities.FirstOrDefaultAsync(n =>
                    n.Id == id && n.CustomerId == customerId);

                if (nc == null)
                    return NotFound("FNC introuvable");

                // Seules les FNC en statut OPEN peuvent être supprimées
                if (nc.Status != "OPEN" && nc.Status != "CANCELLED")
                    return BadRequest("Seules les FNC ouvertes ou annulées peuvent être supprimées");

                // Soft delete
                nc.IsActive = false;
                nc.Status = "CANCELLED";
                nc.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la FNC {NCId}", id);
                return StatusCode(500, "Erreur serveur");
            }
        }
    }
}
