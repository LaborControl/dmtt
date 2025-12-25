using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaborControl.API.Data;
using LaborControl.API.Models;
using System.Security.Claims;

namespace LaborControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class MaintenanceExecutionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public MaintenanceExecutionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private Guid GetCustomerId()
        {
            var customerIdClaim = HttpContext.User.FindFirst("CustomerId")?.Value;
            return Guid.TryParse(customerIdClaim, out var customerId) ? customerId : Guid.Empty;
        }

        // GET: api/maintenanceexecutions
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MaintenanceExecution>>> GetMaintenanceExecutions()
        {
            var customerId = GetCustomerId();

            var executions = await _context.MaintenanceExecutions
                .Where(me => me.CustomerId == customerId)
                .Include(me => me.MaintenanceSchedule)
                .Include(me => me.Asset)
                    .ThenInclude(a => a.Zone)
                        .ThenInclude(z => z.Site)
                .Include(me => me.User)
                .Include(me => me.Team)
                .OrderByDescending(me => me.StartedAt)
                .ToListAsync();

            return Ok(executions);
        }

        // GET: api/maintenanceexecutions/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<MaintenanceExecution>> GetMaintenanceExecution(Guid id)
        {
            var customerId = GetCustomerId();

            var execution = await _context.MaintenanceExecutions
                .Where(me => me.Id == id && me.CustomerId == customerId)
                .Include(me => me.MaintenanceSchedule)
                    .ThenInclude(ms => ms.Tasks)
                .Include(me => me.Asset)
                    .ThenInclude(a => a.Zone)
                        .ThenInclude(z => z.Site)
                .Include(me => me.User)
                .Include(me => me.Team)
                .Include(me => me.ScheduledTask)
                .FirstOrDefaultAsync();

            if (execution == null)
            {
                return NotFound();
            }

            return Ok(execution);
        }

        // GET: api/maintenanceexecutions/asset/{assetId}
        [HttpGet("asset/{assetId}")]
        public async Task<ActionResult<IEnumerable<MaintenanceExecution>>> GetMaintenanceExecutionsByAsset(Guid assetId)
        {
            var customerId = GetCustomerId();

            var executions = await _context.MaintenanceExecutions
                .Where(me => me.AssetId == assetId && me.CustomerId == customerId)
                .Include(me => me.MaintenanceSchedule)
                .Include(me => me.User)
                .Include(me => me.Team)
                .OrderByDescending(me => me.StartedAt)
                .ToListAsync();

            return Ok(executions);
        }

        // GET: api/maintenanceexecutions/user/{userId}
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<MaintenanceExecution>>> GetMaintenanceExecutionsByUser(Guid userId)
        {
            var customerId = GetCustomerId();

            var executions = await _context.MaintenanceExecutions
                .Where(me => me.UserId == userId && me.CustomerId == customerId)
                .Include(me => me.MaintenanceSchedule)
                .Include(me => me.Asset)
                    .ThenInclude(a => a.Zone)
                        .ThenInclude(z => z.Site)
                .OrderByDescending(me => me.StartedAt)
                .ToListAsync();

            return Ok(executions);
        }

        // POST: api/maintenanceexecutions/start
        [HttpPost("start")]
        public async Task<ActionResult<MaintenanceExecution>> StartMaintenance(StartMaintenanceRequest request)
        {
            var customerId = GetCustomerId();

            // Vérifier que la gamme de maintenance existe
            var schedule = await _context.MaintenanceSchedules
                .Where(ms => ms.Id == request.MaintenanceScheduleId && ms.CustomerId == customerId)
                .Include(ms => ms.Asset)
                .FirstOrDefaultAsync();

            if (schedule == null)
            {
                return BadRequest("Gamme de maintenance introuvable");
            }

            // Vérifier que l'utilisateur existe
            var user = await _context.Users
                .Where(u => u.Id == request.UserId && u.CustomerId == customerId)
                .FirstOrDefaultAsync();

            if (user == null)
            {
                return BadRequest("Utilisateur introuvable");
            }

            var execution = new MaintenanceExecution
            {
                Id = Guid.NewGuid(),
                MaintenanceScheduleId = request.MaintenanceScheduleId,
                ScheduledTaskId = request.ScheduledTaskId,
                AssetId = schedule.AssetId,
                UserId = request.UserId,
                CustomerId = customerId,
                TeamId = request.TeamId,
                MaintenanceType = request.MaintenanceType,
                Status = "STARTED",
                StartedAt = DateTime.UtcNow,
                FirstScanAt = request.FirstScanAt.HasValue && request.FirstScanAt.Value.Kind == DateTimeKind.Utc
                    ? request.FirstScanAt
                    : request.FirstScanAt.HasValue ? DateTime.SpecifyKind(request.FirstScanAt.Value, DateTimeKind.Utc) : null
            };

            _context.MaintenanceExecutions.Add(execution);

            // Mettre à jour la tâche planifiée si applicable
            if (request.ScheduledTaskId.HasValue)
            {
                var scheduledTask = await _context.ScheduledTasks
                    .FirstOrDefaultAsync(st => st.Id == request.ScheduledTaskId.Value);

                if (scheduledTask != null)
                {
                    scheduledTask.Status = "IN_PROGRESS";
                    scheduledTask.MaintenanceExecutionId = execution.Id;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(execution);
        }

        // POST: api/maintenanceexecutions/{id}/complete
        [HttpPost("{id}/complete")]
        public async Task<ActionResult<MaintenanceExecution>> CompleteMaintenance(Guid id, CompleteMaintenanceRequest request)
        {
            var customerId = GetCustomerId();

            var execution = await _context.MaintenanceExecutions
                .Where(me => me.Id == id && me.CustomerId == customerId)
                .Include(me => me.MaintenanceSchedule)
                .Include(me => me.ScheduledTask)
                .FirstOrDefaultAsync();

            if (execution == null)
            {
                return NotFound();
            }

            if (execution.Status != "STARTED" && execution.Status != "IN_PROGRESS")
            {
                return BadRequest("Cette maintenance ne peut pas être complétée");
            }

            // Mettre à jour l'exécution
            execution.Status = "COMPLETED";
            execution.CompletedAt = DateTime.UtcNow;
            execution.SecondScanAt = request.SecondScanAt.HasValue && request.SecondScanAt.Value.Kind == DateTimeKind.Utc
                ? request.SecondScanAt
                : request.SecondScanAt.HasValue ? DateTime.SpecifyKind(request.SecondScanAt.Value, DateTimeKind.Utc) : null;
            execution.TaskResults = request.TaskResults ?? "{}";
            execution.GeneralObservations = request.GeneralObservations;
            execution.IssuesFound = request.IssuesFound;
            execution.CorrectiveActions = request.CorrectiveActions;
            execution.Recommendations = request.Recommendations;
            execution.ReplacedParts = request.ReplacedParts;
            execution.ConsumablesUsed = request.ConsumablesUsed;
            execution.Photos = request.Photos;
            execution.TechnicianSignature = request.TechnicianSignature;
            execution.ClientSignature = request.ClientSignature;
            execution.NextMaintenanceRecommended = request.NextMaintenanceRecommended.HasValue && request.NextMaintenanceRecommended.Value.Kind == DateTimeKind.Utc
                ? request.NextMaintenanceRecommended
                : request.NextMaintenanceRecommended.HasValue ? DateTime.SpecifyKind(request.NextMaintenanceRecommended.Value, DateTimeKind.Utc) : null;
            execution.NextMaintenancePriority = request.NextMaintenancePriority;
            execution.EquipmentCondition = request.EquipmentCondition;
            execution.WearPercentage = request.WearPercentage;

            // Calculer la durée réelle
            if (execution.CompletedAt.HasValue)
            {
                execution.ActualDuration = execution.CompletedAt.Value - execution.StartedAt;
            }

            // Initialiser ActualDuration si null
            execution.ActualDuration ??= TimeSpan.Zero;

            // Détection anti-fraude pour la maintenance
            if (execution.FirstScanAt.HasValue && execution.SecondScanAt.HasValue)
            {
                var totalWorkTime = execution.SecondScanAt.Value - execution.FirstScanAt.Value;

                // Détection maintenance trop rapide (moins de 5 minutes)
                if (totalWorkTime.TotalMinutes < 5)
                {
                    execution.FlagSaisieRapide = true;
                }

                // Détection maintenance trop longue (plus de 8 heures)
                if (totalWorkTime.TotalHours > 8)
                {
                    execution.FlagSaisieDifferee = true;
                }
            }

            // Mettre à jour la tâche planifiée
            if (execution.ScheduledTask != null)
            {
                execution.ScheduledTask.Status = "COMPLETED";
            }

            // Mettre à jour la gamme de maintenance
            if (execution.MaintenanceSchedule != null)
            {
                execution.MaintenanceSchedule.LastMaintenanceDate = DateTime.UtcNow;

                // Calculer la prochaine maintenance selon la fréquence
                if (request.NextMaintenanceRecommended.HasValue)
                {
                    execution.MaintenanceSchedule.NextMaintenanceDate = request.NextMaintenanceRecommended?.Kind == DateTimeKind.Utc
                        ? request.NextMaintenanceRecommended
                        : DateTime.SpecifyKind(request.NextMaintenanceRecommended.Value, DateTimeKind.Utc);
                }
                else if (execution.MaintenanceSchedule.LastMaintenanceDate.HasValue)
                {
                    execution.MaintenanceSchedule.NextMaintenanceDate = CalculateNextMaintenanceDate(execution.MaintenanceSchedule);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(execution);
        }

        // POST: api/maintenanceexecutions/{id}/abort
        [HttpPost("{id}/abort")]
        public async Task<ActionResult<MaintenanceExecution>> AbortMaintenance(Guid id, AbortMaintenanceRequest request)
        {
            var customerId = GetCustomerId();

            var execution = await _context.MaintenanceExecutions
                .Where(me => me.Id == id && me.CustomerId == customerId)
                .Include(me => me.ScheduledTask)
                .FirstOrDefaultAsync();

            if (execution == null)
            {
                return NotFound();
            }

            execution.Status = "ABORTED";
            execution.GeneralObservations = request.AbortReason;
            execution.UpdatedAt = DateTime.UtcNow;

            // Mettre à jour la tâche planifiée
            if (execution.ScheduledTask != null)
            {
                execution.ScheduledTask.Status = "CANCELLED";
            }

            await _context.SaveChangesAsync();

            return Ok(execution);
        }

        // Helper method pour calculer la prochaine maintenance
        private DateTime? CalculateNextMaintenanceDate(MaintenanceSchedule schedule)
        {
            if (schedule.LastMaintenanceDate == null) return null;

            return schedule.Frequency switch
            {
                "DAILY" => schedule.LastMaintenanceDate.Value.AddDays(schedule.Interval),
                "WEEKLY" => schedule.LastMaintenanceDate.Value.AddDays(7 * schedule.Interval),
                "MONTHLY" => schedule.LastMaintenanceDate.Value.AddMonths(schedule.Interval),
                "QUARTERLY" => schedule.LastMaintenanceDate.Value.AddMonths(3 * schedule.Interval),
                "YEARLY" => schedule.LastMaintenanceDate.Value.AddYears(schedule.Interval),
                _ => null
            };
        }
    }

    // DTO Classes
    public class StartMaintenanceRequest
    {
        public Guid MaintenanceScheduleId { get; set; }
        public Guid? ScheduledTaskId { get; set; }
        public Guid UserId { get; set; }
        public Guid? TeamId { get; set; }
        public string MaintenanceType { get; set; } = "PREVENTIVE";
        public DateTime? FirstScanAt { get; set; }
    }

    public class CompleteMaintenanceRequest
    {
        public DateTime? SecondScanAt { get; set; }
        public string? TaskResults { get; set; }
        public string? GeneralObservations { get; set; }
        public string? IssuesFound { get; set; }
        public string? CorrectiveActions { get; set; }
        public string? Recommendations { get; set; }
        public string? ReplacedParts { get; set; }
        public string? ConsumablesUsed { get; set; }
        public string? Photos { get; set; }
        public string? TechnicianSignature { get; set; }
        public string? ClientSignature { get; set; }
        public DateTime? NextMaintenanceRecommended { get; set; }
        public string? NextMaintenancePriority { get; set; }
        public string? EquipmentCondition { get; set; }
        public int? WearPercentage { get; set; }
    }

    public class AbortMaintenanceRequest
    {
        public string AbortReason { get; set; } = string.Empty;
    }
}
