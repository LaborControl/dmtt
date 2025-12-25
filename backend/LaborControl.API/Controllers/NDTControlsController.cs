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
    public class NDTControlsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<NDTControlsController> _logger;

        public NDTControlsController(ApplicationDbContext context, ILogger<NDTControlsController> logger)
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

        // GET: api/ndtcontrols
        [HttpGet]
        public async Task<ActionResult<IEnumerable<NDTControlDto>>> GetNDTControls(
            [FromQuery] Guid? weldId = null,
            [FromQuery] string? controlType = null,
            [FromQuery] string? status = null,
            [FromQuery] Guid? controllerId = null,
            [FromQuery] string? result = null)
        {
            try
            {
                var customerId = GetCustomerId();

                var query = _context.NDTControls
                    .Where(c => c.CustomerId == customerId && c.IsActive);

                if (weldId.HasValue)
                    query = query.Where(c => c.WeldId == weldId.Value);

                if (!string.IsNullOrEmpty(controlType))
                    query = query.Where(c => c.ControlType == controlType);

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(c => c.Status == status);

                if (controllerId.HasValue)
                    query = query.Where(c => c.ControllerId == controllerId.Value);

                if (!string.IsNullOrEmpty(result))
                    query = query.Where(c => c.Result == result);

                var controls = await query
                    .AsNoTracking()
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new NDTControlDto
                    {
                        Id = c.Id,
                        WeldId = c.WeldId,
                        WeldReference = c.Weld != null ? c.Weld.Reference : "",
                        NDTProgramId = c.NDTProgramId,
                        NDTProgramReference = c.NDTProgram != null ? c.NDTProgram.Reference : null,
                        ControlType = c.ControlType,
                        ControllerId = c.ControllerId,
                        ControllerName = c.Controller != null
                            ? c.Controller.Prenom + " " + c.Controller.Nom
                            : null,
                        ControllerLevel = c.ControllerLevel,
                        PlannedDate = c.PlannedDate,
                        ControlDate = c.ControlDate,
                        Result = c.Result,
                        Status = c.Status,
                        AppliedStandard = c.AppliedStandard,
                        HasNonConformity = c.NonConformityId.HasValue,
                        CreatedAt = c.CreatedAt
                    })
                    .ToListAsync();

                return Ok(controls);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des contrôles CND");
                return StatusCode(500, "Erreur serveur");
            }
        }

        // GET: api/ndtcontrols/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<NDTControlDetailDto>> GetNDTControl(Guid id)
        {
            try
            {
                var customerId = GetCustomerId();

                var control = await _context.NDTControls
                    .Include(c => c.Weld)
                    .Include(c => c.NDTProgram)
                    .Include(c => c.Controller)
                    .Include(c => c.NonConformity)
                        .ThenInclude(nc => nc!.CreatedBy)
                    .FirstOrDefaultAsync(c => c.Id == id && c.CustomerId == customerId);

                if (control == null)
                    return NotFound("Contrôle CND introuvable");

                var dto = new NDTControlDetailDto
                {
                    Id = control.Id,
                    WeldId = control.WeldId,
                    WeldReference = control.Weld?.Reference ?? "",
                    NDTProgramId = control.NDTProgramId,
                    NDTProgramReference = control.NDTProgram?.Reference,
                    ControlType = control.ControlType,
                    ControllerId = control.ControllerId,
                    ControllerName = control.Controller != null
                        ? $"{control.Controller.Prenom} {control.Controller.Nom}"
                        : null,
                    ControllerLevel = control.ControllerLevel,
                    PlannedDate = control.PlannedDate,
                    ControlDate = control.ControlDate,
                    Result = control.Result,
                    Status = control.Status,
                    AppliedStandard = control.AppliedStandard,
                    AcceptanceCriteria = control.AcceptanceCriteria,
                    Comments = control.Comments,
                    DefectsFound = control.DefectsFound,
                    ControlParameters = control.ControlParameters,
                    Photos = control.Photos,
                    ReportFilePath = control.ReportFilePath,
                    EnvironmentalConditions = control.EnvironmentalConditions,
                    EquipmentUsed = control.EquipmentUsed,
                    EquipmentCalibrationNumber = control.EquipmentCalibrationNumber,
                    FirstScanAt = control.FirstScanAt,
                    SecondScanAt = control.SecondScanAt,
                    NonConformityId = control.NonConformityId,
                    ControllerSignature = control.ControllerSignature,
                    HasNonConformity = control.NonConformityId.HasValue,
                    CreatedAt = control.CreatedAt,
                    UpdatedAt = control.UpdatedAt
                };

                if (control.NonConformity != null)
                {
                    dto.NonConformity = new NonConformityDto
                    {
                        Id = control.NonConformity.Id,
                        Reference = control.NonConformity.Reference,
                        Title = control.NonConformity.Title,
                        Type = control.NonConformity.Type,
                        Severity = control.NonConformity.Severity,
                        Status = control.NonConformity.Status,
                        CreatedById = control.NonConformity.CreatedById,
                        CreatedByName = control.NonConformity.CreatedBy != null
                            ? $"{control.NonConformity.CreatedBy.Prenom} {control.NonConformity.CreatedBy.Nom}"
                            : "",
                        DetectionDate = control.NonConformity.DetectionDate,
                        CreatedAt = control.NonConformity.CreatedAt
                    };
                }

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du contrôle CND {ControlId}", id);
                return StatusCode(500, "Erreur serveur");
            }
        }

        // POST: api/ndtcontrols
        [HttpPost]
        public async Task<ActionResult<NDTControlDto>> CreateNDTControl([FromBody] CreateNDTControlRequest request)
        {
            try
            {
                var customerId = GetCustomerId();

                // Vérifier que la soudure existe
                var weld = await _context.Welds.FindAsync(request.WeldId);
                if (weld == null || weld.CustomerId != customerId)
                    return BadRequest("Soudure introuvable");

                // Vérifier que la soudure est prête pour contrôle
                if (weld.Status != "PENDING_NDT" && weld.Status != "IN_CONTROL")
                    return BadRequest("Cette soudure n'est pas prête pour un contrôle CND");

                var control = new NDTControl
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    WeldId = request.WeldId,
                    NDTProgramId = request.NDTProgramId,
                    ControlType = request.ControlType,
                    ControllerId = request.ControllerId,
                    PlannedDate = request.PlannedDate,
                    AppliedStandard = request.AppliedStandard,
                    AcceptanceCriteria = request.AcceptanceCriteria,
                    Status = "PLANNED",
                    Result = "PENDING",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.NDTControls.Add(control);

                // Mettre à jour le statut de la soudure
                weld.Status = "IN_CONTROL";
                weld.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var dto = new NDTControlDto
                {
                    Id = control.Id,
                    WeldId = control.WeldId,
                    WeldReference = weld.Reference,
                    ControlType = control.ControlType,
                    Status = control.Status,
                    Result = control.Result,
                    CreatedAt = control.CreatedAt
                };

                return CreatedAtAction(nameof(GetNDTControl), new { id = control.Id }, dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du contrôle CND");
                return StatusCode(500, "Erreur serveur");
            }
        }

        // POST: api/ndtcontrols/{id}/execute
        [HttpPost("{id}/execute")]
        public async Task<IActionResult> ExecuteControl(Guid id, [FromBody] NDTControlExecutionRequest request)
        {
            try
            {
                var customerId = GetCustomerId();
                var userId = GetUserId();

                var control = await _context.NDTControls
                    .Include(c => c.Weld)
                    .FirstOrDefaultAsync(c => c.Id == id && c.CustomerId == customerId);

                if (control == null)
                    return NotFound("Contrôle CND introuvable");

                // Vérifier les droits de contrôle CND
                var user = await _context.Users
                    .Include(u => u.WelderQualifications.Where(q => q.IsActive && q.Status == "VALID"))
                    .FirstOrDefaultAsync(u => u.Id == userId);

                if (user == null || !user.CanPerformNDTControls)
                    return Forbid("Vous n'avez pas les droits pour effectuer des contrôles CND");

                // Vérifier que le contrôleur a la qualification pour ce type de contrôle
                var hasQualification = user.WelderQualifications.Any(q =>
                    q.QualificationType == $"NDT_{control.ControlType}" &&
                    q.ExpirationDate > DateTime.UtcNow);

                if (!hasQualification)
                    return BadRequest($"Vous n'avez pas de qualification valide pour le contrôle {control.ControlType}");

                // Récupérer le niveau de certification
                var qualification = user.WelderQualifications
                    .FirstOrDefault(q => q.QualificationType == $"NDT_{control.ControlType}");

                if (request.IsFirstScan)
                {
                    if (control.FirstScanAt.HasValue)
                        return BadRequest("Premier scan déjà effectué");

                    control.FirstScanAt = DateTime.UtcNow;
                    control.ControllerId = userId;
                    control.ControllerLevel = qualification?.CertificationLevel;
                    control.Status = "IN_PROGRESS";
                }
                else
                {
                    if (!control.FirstScanAt.HasValue)
                        return BadRequest("Premier scan non effectué");

                    if (control.SecondScanAt.HasValue)
                        return BadRequest("Second scan déjà effectué");

                    control.SecondScanAt = DateTime.UtcNow;
                    control.ControlDate = DateTime.UtcNow;
                    control.Result = request.Result;
                    control.Comments = request.Comments;
                    control.DefectsFound = request.DefectsFound;
                    control.ControlParameters = request.ControlParameters;
                    control.Photos = request.Photos;
                    control.EnvironmentalConditions = request.EnvironmentalConditions;
                    control.EquipmentUsed = request.EquipmentUsed;
                    control.EquipmentCalibrationNumber = request.EquipmentCalibrationNumber;
                    control.ControllerSignature = request.ControllerSignature;
                    control.Status = "COMPLETED";

                    // Créer une FNC si non conforme
                    if (request.Result == "NON_CONFORM" && request.CreateNonConformity)
                    {
                        var fnc = new NonConformity
                        {
                            Id = Guid.NewGuid(),
                            CustomerId = customerId,
                            Reference = $"FNC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..4].ToUpper()}",
                            Title = $"Non-conformité détectée - Contrôle {control.ControlType}",
                            Type = "WELD_DEFECT",
                            Severity = "MAJOR",
                            WeldId = control.WeldId,
                            Description = request.NonConformityDescription ?? $"Défauts détectés lors du contrôle {control.ControlType}: {request.DefectsFound}",
                            Status = "OPEN",
                            CreatedById = userId,
                            DetectionDate = DateTime.UtcNow,
                            RequiresRecontrol = true,
                            IsActive = true,
                            CreatedAt = DateTime.UtcNow
                        };

                        _context.NonConformities.Add(fnc);
                        control.NonConformityId = fnc.Id;

                        // Mettre à jour la soudure
                        if (control.Weld != null)
                        {
                            control.Weld.Status = "NON_CONFORM";
                            control.Weld.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                    else if (request.Result == "CONFORM")
                    {
                        // Vérifier si tous les contrôles sont terminés et conformes
                        var allControlsComplete = await _context.NDTControls
                            .Where(c => c.WeldId == control.WeldId && c.IsActive && c.Id != control.Id)
                            .AllAsync(c => c.Status == "COMPLETED" && c.Result == "CONFORM");

                        if (allControlsComplete && control.Weld != null)
                        {
                            control.Weld.Status = "COMPLETED";
                            control.Weld.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }

                control.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new {
                    Message = request.IsFirstScan ? "Début de contrôle enregistré" : "Contrôle terminé",
                    Status = control.Status,
                    Result = control.Result,
                    NonConformityCreated = control.NonConformityId.HasValue
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'exécution du contrôle CND {ControlId}", id);
                return StatusCode(500, "Erreur serveur");
            }
        }

        // GET: api/ndtcontrols/dashboard
        [HttpGet("dashboard")]
        public async Task<ActionResult<NDTDashboardDto>> GetDashboard()
        {
            try
            {
                var customerId = GetCustomerId();

                var controls = await _context.NDTControls
                    .Where(c => c.CustomerId == customerId && c.IsActive)
                    .ToListAsync();

                var dashboard = new NDTDashboardDto
                {
                    TotalControls = controls.Count,
                    PlannedControls = controls.Count(c => c.Status == "PLANNED"),
                    InProgressControls = controls.Count(c => c.Status == "IN_PROGRESS"),
                    CompletedControls = controls.Count(c => c.Status == "COMPLETED"),
                    ConformControls = controls.Count(c => c.Result == "CONFORM"),
                    NonConformControls = controls.Count(c => c.Result == "NON_CONFORM"),
                    TypeBreakdown = controls
                        .GroupBy(c => c.ControlType)
                        .Select(g => new NDTTypeCount
                        {
                            ControlType = g.Key,
                            Total = g.Count(),
                            Conform = g.Count(c => c.Result == "CONFORM"),
                            NonConform = g.Count(c => c.Result == "NON_CONFORM")
                        })
                        .ToList()
                };

                var completed = dashboard.CompletedControls;
                dashboard.ConformityRate = completed > 0
                    ? Math.Round((decimal)dashboard.ConformControls / completed * 100, 2)
                    : 0;

                return Ok(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du dashboard CND");
                return StatusCode(500, "Erreur serveur");
            }
        }

        // DELETE: api/ndtcontrols/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteNDTControl(Guid id)
        {
            try
            {
                var customerId = GetCustomerId();

                var control = await _context.NDTControls.FirstOrDefaultAsync(c =>
                    c.Id == id && c.CustomerId == customerId);

                if (control == null)
                    return NotFound("Contrôle CND introuvable");

                if (control.Status == "COMPLETED")
                    return BadRequest("Impossible de supprimer un contrôle terminé");

                if (control.NonConformityId.HasValue)
                    return BadRequest("Impossible de supprimer un contrôle avec une FNC associée");

                // Soft delete
                control.IsActive = false;
                control.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du contrôle CND {ControlId}", id);
                return StatusCode(500, "Erreur serveur");
            }
        }
    }
}
