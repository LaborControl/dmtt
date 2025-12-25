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
    public class WeldsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<WeldsController> _logger;

        public WeldsController(ApplicationDbContext context, ILogger<WeldsController> logger)
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

        // GET: api/welds
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WeldDto>>> GetWelds(
            [FromQuery] Guid? assetId = null,
            [FromQuery] string? status = null,
            [FromQuery] Guid? welderId = null,
            [FromQuery] bool? isBlocked = null)
        {
            try
            {
                var customerId = GetCustomerId();

                var query = _context.Welds
                    .Where(w => w.CustomerId == customerId && w.IsActive);

                if (assetId.HasValue)
                    query = query.Where(w => w.AssetId == assetId.Value);

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(w => w.Status == status);

                if (welderId.HasValue)
                    query = query.Where(w => w.WelderId == welderId.Value);

                if (isBlocked.HasValue)
                    query = query.Where(w => w.IsBlocked == isBlocked.Value);

                var welds = await query
                    .AsNoTracking()
                    .OrderByDescending(w => w.CreatedAt)
                    .Select(w => new WeldDto
                    {
                        Id = w.Id,
                        Reference = w.Reference,
                        AssetId = w.AssetId,
                        AssetName = w.Asset != null ? w.Asset.Name : "",
                        Diameter = w.Diameter,
                        Thickness = w.Thickness,
                        Material1 = w.Material1,
                        Material2 = w.Material2,
                        WeldingProcess = w.WeldingProcess,
                        JointType = w.JointType,
                        WeldClass = w.WeldClass,
                        WeldingPosition = w.WeldingPosition,
                        DMOSId = w.DMOSId,
                        DMOSReference = w.DMOS != null ? w.DMOS.Reference : null,
                        WelderId = w.WelderId,
                        WelderName = w.Welder != null ? w.Welder.Prenom + " " + w.Welder.Nom : null,
                        PlannedDate = w.PlannedDate,
                        ExecutionDate = w.ExecutionDate,
                        Status = w.Status,
                        IsBlocked = w.IsBlocked,
                        IsCCPUValidated = w.CCPUValidatorId.HasValue,
                        NDTControlsCount = w.NDTControls.Count(c => c.IsActive),
                        NonConformitiesCount = w.NonConformities.Count(nc => nc.IsActive),
                        CreatedAt = w.CreatedAt
                    })
                    .ToListAsync();

                return Ok(welds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des soudures");
                return StatusCode(500, "Erreur serveur");
            }
        }

        // GET: api/welds/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<WeldDetailDto>> GetWeld(Guid id)
        {
            try
            {
                var customerId = GetCustomerId();

                var weld = await _context.Welds
                    .Include(w => w.Asset)
                    .Include(w => w.DMOS)
                    .Include(w => w.Welder)
                    .Include(w => w.CCPUValidator)
                    .Include(w => w.NDTControls.Where(c => c.IsActive))
                        .ThenInclude(c => c.Controller)
                    .Include(w => w.NonConformities.Where(nc => nc.IsActive))
                        .ThenInclude(nc => nc.CreatedBy)
                    .FirstOrDefaultAsync(w => w.Id == id && w.CustomerId == customerId);

                if (weld == null)
                    return NotFound("Soudure introuvable");

                var dto = new WeldDetailDto
                {
                    Id = weld.Id,
                    Reference = weld.Reference,
                    AssetId = weld.AssetId,
                    AssetName = weld.Asset?.Name ?? "",
                    Diameter = weld.Diameter,
                    Thickness = weld.Thickness,
                    Material1 = weld.Material1,
                    Material2 = weld.Material2,
                    WeldingProcess = weld.WeldingProcess,
                    JointType = weld.JointType,
                    WeldClass = weld.WeldClass,
                    WeldingPosition = weld.WeldingPosition,
                    DMOSId = weld.DMOSId,
                    DMOSReference = weld.DMOS?.Reference,
                    WelderId = weld.WelderId,
                    WelderName = weld.Welder != null ? $"{weld.Welder.Prenom} {weld.Welder.Nom}" : null,
                    PlannedDate = weld.PlannedDate,
                    ExecutionDate = weld.ExecutionDate,
                    Status = weld.Status,
                    IsBlocked = weld.IsBlocked,
                    IsCCPUValidated = weld.CCPUValidatorId.HasValue,
                    CCPUValidatorId = weld.CCPUValidatorId,
                    CCPUValidatorName = weld.CCPUValidator != null ? $"{weld.CCPUValidator.Prenom} {weld.CCPUValidator.Nom}" : null,
                    CCPUValidationDate = weld.CCPUValidationDate,
                    CCPUComments = weld.CCPUComments,
                    BlockReason = weld.BlockReason,
                    WeldingParameters = weld.WeldingParameters,
                    Photos = weld.Photos,
                    WelderObservations = weld.WelderObservations,
                    FirstScanAt = weld.FirstScanAt,
                    SecondScanAt = weld.SecondScanAt,
                    CreatedAt = weld.CreatedAt,
                    UpdatedAt = weld.UpdatedAt,
                    NDTControlsCount = weld.NDTControls.Count,
                    NonConformitiesCount = weld.NonConformities.Count,
                    NDTControls = weld.NDTControls.Select(c => new NDTControlDto
                    {
                        Id = c.Id,
                        WeldId = c.WeldId,
                        WeldReference = weld.Reference,
                        ControlType = c.ControlType,
                        ControllerId = c.ControllerId,
                        ControllerName = c.Controller != null ? $"{c.Controller.Prenom} {c.Controller.Nom}" : null,
                        ControllerLevel = c.ControllerLevel,
                        PlannedDate = c.PlannedDate,
                        ControlDate = c.ControlDate,
                        Result = c.Result,
                        Status = c.Status,
                        AppliedStandard = c.AppliedStandard,
                        HasNonConformity = c.NonConformityId.HasValue,
                        CreatedAt = c.CreatedAt
                    }).ToList(),
                    NonConformities = weld.NonConformities.Select(nc => new NonConformityDto
                    {
                        Id = nc.Id,
                        Reference = nc.Reference,
                        Title = nc.Title,
                        Type = nc.Type,
                        Severity = nc.Severity,
                        WeldId = nc.WeldId,
                        WeldReference = weld.Reference,
                        Status = nc.Status,
                        CreatedById = nc.CreatedById,
                        CreatedByName = nc.CreatedBy != null ? $"{nc.CreatedBy.Prenom} {nc.CreatedBy.Nom}" : "",
                        DetectionDate = nc.DetectionDate,
                        DueDate = nc.DueDate,
                        RequiresRecontrol = nc.RequiresRecontrol,
                        CreatedAt = nc.CreatedAt
                    }).ToList()
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la soudure {WeldId}", id);
                return StatusCode(500, "Erreur serveur");
            }
        }

        // POST: api/welds
        [HttpPost]
        public async Task<ActionResult<WeldDto>> CreateWeld([FromBody] CreateWeldRequest request)
        {
            try
            {
                var customerId = GetCustomerId();

                // Vérifier unicité de la référence
                var exists = await _context.Welds.AnyAsync(w =>
                    w.CustomerId == customerId && w.Reference == request.Reference);
                if (exists)
                    return BadRequest("Une soudure avec cette référence existe déjà");

                // Vérifier que l'asset existe
                var asset = await _context.Assets.FindAsync(request.AssetId);
                if (asset == null)
                    return BadRequest("Équipement introuvable");

                var weld = new Weld
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    Reference = request.Reference,
                    AssetId = request.AssetId,
                    Diameter = request.Diameter,
                    Thickness = request.Thickness,
                    Material1 = request.Material1,
                    Material2 = request.Material2,
                    WeldingProcess = request.WeldingProcess,
                    JointType = request.JointType,
                    WeldClass = request.WeldClass,
                    WeldingPosition = request.WeldingPosition,
                    DMOSId = request.DMOSId,
                    WelderId = request.WelderId,
                    PlannedDate = request.PlannedDate,
                    WeldingParameters = request.WeldingParameters,
                    Status = "PLANNED",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Welds.Add(weld);
                await _context.SaveChangesAsync();

                var dto = new WeldDto
                {
                    Id = weld.Id,
                    Reference = weld.Reference,
                    AssetId = weld.AssetId,
                    AssetName = asset.Name,
                    WeldingProcess = weld.WeldingProcess,
                    JointType = weld.JointType,
                    Status = weld.Status,
                    CreatedAt = weld.CreatedAt
                };

                return CreatedAtAction(nameof(GetWeld), new { id = weld.Id }, dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la soudure");
                return StatusCode(500, "Erreur serveur");
            }
        }

        // PUT: api/welds/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateWeld(Guid id, [FromBody] UpdateWeldRequest request)
        {
            try
            {
                var customerId = GetCustomerId();

                var weld = await _context.Welds.FirstOrDefaultAsync(w =>
                    w.Id == id && w.CustomerId == customerId);

                if (weld == null)
                    return NotFound("Soudure introuvable");

                // Vérifier si la soudure est verrouillée
                if (weld.IsBlocked)
                    return BadRequest("Cette soudure est verrouillée");

                // Vérifier si le statut permet la modification
                if (weld.Status == "COMPLETED" || weld.Status == "FINAL_VALIDATED")
                    return BadRequest("Impossible de modifier une soudure terminée ou validée");

                weld.AssetId = request.AssetId;
                weld.Diameter = request.Diameter;
                weld.Thickness = request.Thickness;
                weld.Material1 = request.Material1;
                weld.Material2 = request.Material2;
                weld.WeldingProcess = request.WeldingProcess;
                weld.JointType = request.JointType;
                weld.WeldClass = request.WeldClass;
                weld.WeldingPosition = request.WeldingPosition;
                weld.DMOSId = request.DMOSId;
                weld.WelderId = request.WelderId;
                weld.PlannedDate = request.PlannedDate;
                weld.WeldingParameters = request.WeldingParameters;
                weld.WelderObservations = request.WelderObservations;
                weld.Status = request.Status;
                weld.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la soudure {WeldId}", id);
                return StatusCode(500, "Erreur serveur");
            }
        }

        // POST: api/welds/{id}/ccpu-validation
        [HttpPost("{id}/ccpu-validation")]
        public async Task<IActionResult> CCPUValidation(Guid id, [FromBody] CCPUValidationRequest request)
        {
            try
            {
                var customerId = GetCustomerId();
                var userId = GetUserId();

                var weld = await _context.Welds.FirstOrDefaultAsync(w =>
                    w.Id == id && w.CustomerId == customerId);

                if (weld == null)
                    return NotFound("Soudure introuvable");

                // Vérifier les droits CCPU
                var user = await _context.Users.FindAsync(userId);
                if (user == null || !user.CanValidateAsCCPU)
                    return Forbid("Vous n'avez pas les droits CCPU");

                if (request.Approve)
                {
                    weld.CCPUValidatorId = userId;
                    weld.CCPUValidationDate = DateTime.UtcNow;
                    weld.CCPUComments = request.Comments;
                    weld.Status = "CCPU_VALIDATED";
                    weld.IsBlocked = false;
                    weld.BlockReason = null;
                }
                else
                {
                    weld.IsBlocked = true;
                    weld.BlockReason = request.BlockReason ?? "Refusé par CCPU";
                    weld.CCPUComments = request.Comments;
                    weld.Status = "BLOCKED";
                }

                weld.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new {
                    Message = request.Approve ? "Soudure validée par CCPU" : "Soudure refusée par CCPU",
                    Status = weld.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la validation CCPU de la soudure {WeldId}", id);
                return StatusCode(500, "Erreur serveur");
            }
        }

        // POST: api/welds/{id}/execution-scan
        [HttpPost("{id}/execution-scan")]
        public async Task<IActionResult> ExecutionScan(Guid id, [FromBody] WeldExecutionScanRequest request)
        {
            try
            {
                var customerId = GetCustomerId();
                var userId = GetUserId();

                var weld = await _context.Welds.FirstOrDefaultAsync(w =>
                    w.Id == id && w.CustomerId == customerId);

                if (weld == null)
                    return NotFound("Soudure introuvable");

                if (weld.IsBlocked)
                    return BadRequest("Cette soudure est verrouillée");

                // Vérifier validation CCPU
                if (!weld.CCPUValidatorId.HasValue)
                    return BadRequest("Cette soudure n'a pas été validée par le CCPU");

                var user = await _context.Users.FindAsync(userId);
                if (user == null || !user.CanPerformNuclearWelds)
                    return Forbid("Vous n'avez pas les droits pour souder");

                if (request.IsFirstScan)
                {
                    if (weld.FirstScanAt.HasValue)
                        return BadRequest("Premier scan déjà effectué");

                    weld.FirstScanAt = DateTime.UtcNow;
                    weld.WelderId = userId;
                    weld.Status = "IN_PROGRESS";
                }
                else
                {
                    if (!weld.FirstScanAt.HasValue)
                        return BadRequest("Premier scan non effectué");

                    if (weld.SecondScanAt.HasValue)
                        return BadRequest("Second scan déjà effectué");

                    weld.SecondScanAt = DateTime.UtcNow;
                    weld.ExecutionDate = DateTime.UtcNow;
                    weld.WelderObservations = request.WelderObservations;
                    weld.Photos = request.Photos;
                    weld.Status = "PENDING_NDT";
                }

                weld.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new {
                    Message = request.IsFirstScan ? "Début de soudage enregistré" : "Fin de soudage enregistrée",
                    Status = weld.Status,
                    FirstScanAt = weld.FirstScanAt,
                    SecondScanAt = weld.SecondScanAt
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du scan d'exécution de la soudure {WeldId}", id);
                return StatusCode(500, "Erreur serveur");
            }
        }

        // GET: api/welds/dashboard
        [HttpGet("dashboard")]
        public async Task<ActionResult<WeldDashboardDto>> GetDashboard()
        {
            try
            {
                var customerId = GetCustomerId();

                var welds = await _context.Welds
                    .Where(w => w.CustomerId == customerId && w.IsActive)
                    .ToListAsync();

                var dashboard = new WeldDashboardDto
                {
                    TotalWelds = welds.Count,
                    PlannedWelds = welds.Count(w => w.Status == "PLANNED"),
                    InProgressWelds = welds.Count(w => w.Status == "IN_PROGRESS"),
                    CompletedWelds = welds.Count(w => w.Status == "COMPLETED" || w.Status == "FINAL_VALIDATED"),
                    PendingCCPUValidation = welds.Count(w => w.Status == "PENDING_CCPU" || (!w.CCPUValidatorId.HasValue && w.Status == "PLANNED")),
                    PendingNDTControl = welds.Count(w => w.Status == "PENDING_NDT"),
                    WithNonConformities = await _context.NonConformities
                        .Where(nc => nc.CustomerId == customerId && nc.WeldId.HasValue && nc.IsActive && nc.Status != "CLOSED")
                        .Select(nc => nc.WeldId)
                        .Distinct()
                        .CountAsync(),
                    BlockedWelds = welds.Count(w => w.IsBlocked),
                    StatusBreakdown = welds
                        .GroupBy(w => w.Status)
                        .Select(g => new WeldStatusCount { Status = g.Key, Count = g.Count() })
                        .ToList()
                };

                var totalCompleted = welds.Count(w => w.Status == "COMPLETED" || w.Status == "FINAL_VALIDATED");
                var conformWelds = totalCompleted - dashboard.WithNonConformities;
                dashboard.ConformityRate = totalCompleted > 0
                    ? Math.Round((decimal)conformWelds / totalCompleted * 100, 2)
                    : 0;

                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du dashboard soudures");
                return StatusCode(500, "Erreur serveur");
            }
        }

        // DELETE: api/welds/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteWeld(Guid id)
        {
            try
            {
                var customerId = GetCustomerId();

                var weld = await _context.Welds
                    .Include(w => w.NDTControls)
                    .Include(w => w.NonConformities)
                    .FirstOrDefaultAsync(w => w.Id == id && w.CustomerId == customerId);

                if (weld == null)
                    return NotFound("Soudure introuvable");

                // Vérifier qu'il n'y a pas de contrôles CND actifs
                if (weld.NDTControls.Any(c => c.IsActive))
                    return BadRequest("Impossible de supprimer une soudure avec des contrôles CND");

                // Vérifier qu'il n'y a pas de FNC actives
                if (weld.NonConformities.Any(nc => nc.IsActive && nc.Status != "CLOSED"))
                    return BadRequest("Impossible de supprimer une soudure avec des FNC ouvertes");

                // Soft delete
                weld.IsActive = false;
                weld.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la soudure {WeldId}", id);
                return StatusCode(500, "Erreur serveur");
            }
        }
    }
}
