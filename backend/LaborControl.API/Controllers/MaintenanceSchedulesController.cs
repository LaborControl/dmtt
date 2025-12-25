using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaborControl.API.Data;
using LaborControl.API.Models;
using System.Security.Claims;

namespace LaborControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MaintenanceSchedulesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MaintenanceSchedulesController(ApplicationDbContext context)
        {
            _context = context;
        }

        private Guid GetCustomerId()
        {
            var customerIdClaim = HttpContext.User.FindFirst("CustomerId")?.Value;
            return Guid.TryParse(customerIdClaim, out var customerId) ? customerId : Guid.Empty;
        }

        // GET: api/maintenanceschedules
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MaintenanceSchedule>>> GetMaintenanceSchedules()
        {
            var customerId = GetCustomerId();

            var schedules = await _context.MaintenanceSchedules
                .Where(ms => ms.CustomerId == customerId)
                .Include(ms => ms.Asset)
                    .ThenInclude(a => a.Zone)
                        .ThenInclude(z => z.Site)
                .Include(ms => ms.DefaultTeam)
                .Include(ms => ms.DefaultUser)
                .Include(ms => ms.Tasks)
                .Include(ms => ms.MaintenanceScheduleQualifications)
                    .ThenInclude(msq => msq.Qualification)
                .OrderBy(ms => ms.Asset.Name)
                .ThenBy(ms => ms.Name)
                .ToListAsync();

            return Ok(schedules);
        }

        // GET: api/maintenanceschedules/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<MaintenanceSchedule>> GetMaintenanceSchedule(Guid id)
        {
            var customerId = GetCustomerId();

            var schedule = await _context.MaintenanceSchedules
                .Where(ms => ms.Id == id && ms.CustomerId == customerId)
                .Include(ms => ms.Asset)
                    .ThenInclude(a => a.Zone)
                        .ThenInclude(z => z.Site)
                .Include(ms => ms.DefaultTeam)
                .Include(ms => ms.DefaultUser)
                .Include(ms => ms.Tasks.OrderBy(t => t.OrderIndex))
                .Include(ms => ms.Executions.OrderByDescending(e => e.StartedAt))
                .Include(ms => ms.MaintenanceScheduleQualifications)
                    .ThenInclude(msq => msq.Qualification)
                .FirstOrDefaultAsync();

            if (schedule == null)
            {
                return NotFound();
            }

            return Ok(schedule);
        }

        // GET: api/maintenanceschedules/asset/{assetId}
        [HttpGet("asset/{assetId}")]
        public async Task<ActionResult<IEnumerable<MaintenanceSchedule>>> GetMaintenanceSchedulesByAsset(Guid assetId)
        {
            var customerId = GetCustomerId();

            var schedules = await _context.MaintenanceSchedules
                .Where(ms => ms.AssetId == assetId && ms.CustomerId == customerId)
                .Include(ms => ms.Tasks.OrderBy(t => t.OrderIndex))
                .Include(ms => ms.DefaultTeam)
                .Include(ms => ms.DefaultUser)
                .Include(ms => ms.MaintenanceScheduleQualifications)
                    .ThenInclude(msq => msq.Qualification)
                .OrderBy(ms => ms.Priority == "CRITICAL" ? 0 : ms.Priority == "HIGH" ? 1 : ms.Priority == "NORMAL" ? 2 : 3)
                .ThenBy(ms => ms.Name)
                .ToListAsync();

            return Ok(schedules);
        }

        // POST: api/maintenanceschedules
        [HttpPost]
        public async Task<ActionResult<MaintenanceSchedule>> CreateMaintenanceSchedule(CreateMaintenanceScheduleRequest request)
        {
            var customerId = GetCustomerId();

            // Vérifier que l'équipement existe et appartient au customer
            var asset = await _context.Assets
                .Include(a => a.Zone)
                .Where(a => a.Id == request.AssetId && a.Zone.Site.CustomerId == customerId)
                .FirstOrDefaultAsync();

            if (asset == null)
            {
                return BadRequest("Équipement introuvable");
            }

            var schedule = new MaintenanceSchedule
            {
                Id = Guid.NewGuid(),
                AssetId = request.AssetId,
                CustomerId = customerId,
                Name = request.Name,
                Description = request.Description,
                Type = request.Type,
                Priority = request.Priority,
                Frequency = request.Frequency,
                Interval = request.Interval,
                OperatingHoursInterval = request.OperatingHoursInterval,
                EstimatedDurationMinutes = request.EstimatedDurationMinutes,
                RequiredQualification = request.RequiredQualification,
                DefaultTeamId = request.DefaultTeamId,
                DefaultUserId = request.DefaultUserId,
                NextMaintenanceDate = request.NextMaintenanceDate.HasValue && request.NextMaintenanceDate.Value.Kind == DateTimeKind.Utc
                    ? request.NextMaintenanceDate
                    : DateTime.SpecifyKind(request.NextMaintenanceDate ?? DateTime.UtcNow, DateTimeKind.Utc),
                SpecialInstructions = request.SpecialInstructions,
                SpareParts = request.SpareParts,
                RequiredTools = request.RequiredTools,
                Status = "ACTIVE",
                IsAiGenerated = request.IsAiGenerated,
                ManufacturerData = request.ManufacturerData
            };

            _context.MaintenanceSchedules.Add(schedule);

            // Ajouter les qualifications
            if (request.QualificationIds != null && request.QualificationIds.Any())
            {
                foreach (var qualificationId in request.QualificationIds)
                {
                    var msQualification = new MaintenanceScheduleQualification
                    {
                        Id = Guid.NewGuid(),
                        MaintenanceScheduleId = schedule.Id,
                        QualificationId = qualificationId,
                        IsMandatory = true,
                        AlertLevel = 3
                    };
                    _context.MaintenanceScheduleQualifications.Add(msQualification);
                }
            }

            // Ajouter les tâches
            if (request.Tasks != null && request.Tasks.Any())
            {
                foreach (var taskRequest in request.Tasks)
                {
                    var task = new MaintenanceTask
                    {
                        Id = Guid.NewGuid(),
                        MaintenanceScheduleId = schedule.Id,
                        Name = taskRequest.Name,
                        Description = taskRequest.Description,
                        TaskType = taskRequest.TaskType,
                        OrderIndex = taskRequest.OrderIndex,
                        EstimatedDurationMinutes = taskRequest.EstimatedDurationMinutes,
                        Instructions = taskRequest.Instructions,
                        AcceptanceCriteria = taskRequest.AcceptanceCriteria,
                        SpecificQualification = taskRequest.SpecificQualification,
                        SpecificTools = taskRequest.SpecificTools,
                        SpecificParts = taskRequest.SpecificParts,
                        IsMandatory = taskRequest.IsMandatory,
                        RequiresPhoto = taskRequest.RequiresPhoto,
                        RequiresMeasurement = taskRequest.RequiresMeasurement,
                        MeasurementUnit = taskRequest.MeasurementUnit,
                        MinValue = taskRequest.MinValue,
                        MaxValue = taskRequest.MaxValue,
                        SafetyInstructions = taskRequest.SafetyInstructions,
                        IsAiGenerated = taskRequest.IsAiGenerated
                    };

                    _context.MaintenanceTasks.Add(task);
                }
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetMaintenanceSchedule), new { id = schedule.Id }, schedule);
        }

        // PUT: api/maintenanceschedules/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMaintenanceSchedule(Guid id, UpdateMaintenanceScheduleRequest request)
        {
            var customerId = GetCustomerId();

            var schedule = await _context.MaintenanceSchedules
                .Where(ms => ms.Id == id && ms.CustomerId == customerId)
                .Include(ms => ms.MaintenanceScheduleQualifications)
                .FirstOrDefaultAsync();

            if (schedule == null)
            {
                return NotFound();
            }

            schedule.Name = request.Name;
            schedule.Description = request.Description;
            schedule.Type = request.Type;
            schedule.Priority = request.Priority;
            schedule.Frequency = request.Frequency;
            schedule.Interval = request.Interval;
            schedule.OperatingHoursInterval = request.OperatingHoursInterval;
            schedule.EstimatedDurationMinutes = request.EstimatedDurationMinutes;
            schedule.RequiredQualification = request.RequiredQualification;
            schedule.DefaultTeamId = request.DefaultTeamId;
            schedule.DefaultUserId = request.DefaultUserId;
            schedule.NextMaintenanceDate = request.NextMaintenanceDate.HasValue && request.NextMaintenanceDate.Value.Kind == DateTimeKind.Utc
                ? request.NextMaintenanceDate
                : DateTime.SpecifyKind(request.NextMaintenanceDate ?? DateTime.UtcNow, DateTimeKind.Utc);
            schedule.SpecialInstructions = request.SpecialInstructions;
            schedule.SpareParts = request.SpareParts;
            schedule.RequiredTools = request.RequiredTools;
            schedule.Status = request.Status;
            schedule.UpdatedAt = DateTime.UtcNow;

            // Mettre à jour les qualifications
            if (request.QualificationIds != null)
            {
                // Supprimer les anciennes associations
                var existingQualifications = schedule.MaintenanceScheduleQualifications.ToList();
                _context.MaintenanceScheduleQualifications.RemoveRange(existingQualifications);

                // Ajouter les nouvelles associations
                foreach (var qualificationId in request.QualificationIds)
                {
                    var msQualification = new MaintenanceScheduleQualification
                    {
                        Id = Guid.NewGuid(),
                        MaintenanceScheduleId = schedule.Id,
                        QualificationId = qualificationId,
                        IsMandatory = true,
                        AlertLevel = 3
                    };
                    _context.MaintenanceScheduleQualifications.Add(msQualification);
                }
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/maintenanceschedules/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMaintenanceSchedule(Guid id)
        {
            var customerId = GetCustomerId();

            var schedule = await _context.MaintenanceSchedules
                .Where(ms => ms.Id == id && ms.CustomerId == customerId)
                .FirstOrDefaultAsync();

            if (schedule == null)
            {
                return NotFound();
            }

            // Vérifier s'il y a des exécutions en cours
            var hasActiveExecutions = await _context.MaintenanceExecutions
                .AnyAsync(me => me.MaintenanceScheduleId == id && me.Status != "COMPLETED" && me.Status != "ABORTED");

            if (hasActiveExecutions)
            {
                return BadRequest("Impossible de supprimer une gamme avec des maintenances en cours");
            }

            _context.MaintenanceSchedules.Remove(schedule);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/maintenanceschedules/{id}/activate
        [HttpPost("{id}/activate")]
        public async Task<IActionResult> ActivateMaintenanceSchedule(Guid id)
        {
            var customerId = GetCustomerId();

            var schedule = await _context.MaintenanceSchedules
                .Where(ms => ms.Id == id && ms.CustomerId == customerId)
                .FirstOrDefaultAsync();

            if (schedule == null)
            {
                return NotFound();
            }

            schedule.Status = "ACTIVE";
            schedule.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Gamme de maintenance activée" });
        }

        // POST: api/maintenanceschedules/{id}/deactivate
        [HttpPost("{id}/deactivate")]
        public async Task<IActionResult> DeactivateMaintenanceSchedule(Guid id)
        {
            var customerId = GetCustomerId();

            var schedule = await _context.MaintenanceSchedules
                .Where(ms => ms.Id == id && ms.CustomerId == customerId)
                .FirstOrDefaultAsync();

            if (schedule == null)
            {
                return NotFound();
            }

            schedule.Status = "INACTIVE";
            schedule.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(new { message = "Gamme de maintenance désactivée" });
        }

        // GET: api/maintenanceschedules/overdue/count
        [HttpGet("overdue/count")]
        public async Task<ActionResult<int>> GetOverdueMaintenanceCount()
        {
            var customerId = GetCustomerId();
            var today = DateTime.UtcNow.Date;

            var count = await _context.MaintenanceSchedules
                .Where(ms => ms.CustomerId == customerId &&
                             ms.Status == "ACTIVE" &&
                             ms.NextMaintenanceDate.HasValue &&
                             ms.NextMaintenanceDate.Value.Date < today)
                .CountAsync();

            return Ok(count);
        }
    }

    // DTO Classes
    public class CreateMaintenanceScheduleRequest
    {
        public Guid AssetId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Type { get; set; } = "PREVENTIVE";
        public string Priority { get; set; } = "NORMAL";
        public string Frequency { get; set; } = "MONTHLY";
        public int Interval { get; set; } = 1;
        public int? OperatingHoursInterval { get; set; }
        public int EstimatedDurationMinutes { get; set; } = 60;
        public string RequiredQualification { get; set; } = "TECH_MAINTENANCE";
        public Guid? DefaultTeamId { get; set; }
        public Guid? DefaultUserId { get; set; }
        public DateTime? NextMaintenanceDate { get; set; }
        public string? SpecialInstructions { get; set; }
        public string? SpareParts { get; set; }
        public string? RequiredTools { get; set; }
        public bool IsAiGenerated { get; set; } = false;
        public string? ManufacturerData { get; set; }
        public List<CreateMaintenanceTaskRequest>? Tasks { get; set; }
        public List<Guid>? QualificationIds { get; set; } // Multi-qualifications
    }

    public class UpdateMaintenanceScheduleRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string Type { get; set; } = "PREVENTIVE";
        public string Priority { get; set; } = "NORMAL";
        public string Frequency { get; set; } = "MONTHLY";
        public int Interval { get; set; } = 1;
        public int? OperatingHoursInterval { get; set; }
        public int EstimatedDurationMinutes { get; set; } = 60;
        public string RequiredQualification { get; set; } = "TECH_MAINTENANCE";
        public Guid? DefaultTeamId { get; set; }
        public Guid? DefaultUserId { get; set; }
        public DateTime? NextMaintenanceDate { get; set; }
        public string? SpecialInstructions { get; set; }
        public string? SpareParts { get; set; }
        public string? RequiredTools { get; set; }
        public string Status { get; set; } = "ACTIVE";
        public List<Guid>? QualificationIds { get; set; } // Multi-qualifications
    }

    public class CreateMaintenanceTaskRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string TaskType { get; set; } = "CHECK";
        public int OrderIndex { get; set; } = 0;
        public int EstimatedDurationMinutes { get; set; } = 10;
        public string? Instructions { get; set; }
        public string? AcceptanceCriteria { get; set; }
        public string? SpecificQualification { get; set; }
        public string? SpecificTools { get; set; }
        public string? SpecificParts { get; set; }
        public bool IsMandatory { get; set; } = true;
        public bool RequiresPhoto { get; set; } = false;
        public bool RequiresMeasurement { get; set; } = false;
        public string? MeasurementUnit { get; set; }
        public decimal? MinValue { get; set; }
        public decimal? MaxValue { get; set; }
        public string? SafetyInstructions { get; set; }
        public bool IsAiGenerated { get; set; } = false;
    }
}
