using System.Security.Claims;
using LaborControl.API.Data;
using LaborControl.API.Models;
using LaborControl.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaborControl.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class TasksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IEmailService _emailService;
        private readonly ILogger<TasksController> _logger;
        private static readonly Guid DEFAULT_TASK_TEMPLATE_ID = new Guid("a0000000-0000-0000-0000-000000000001");

        public TasksController(ApplicationDbContext context, IEmailService emailService, ILogger<TasksController> logger)
        {
            _context = context;
            _emailService = emailService;
            _logger = logger;
        }

        private Guid GetCustomerId()
        {
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;
            return Guid.Parse(customerIdClaim!);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<ScheduledTask>>> GetTasks()
        {
            var customerId = GetCustomerId();
            // Optimisation: AsNoTracking pour lecture seule + AsSplitQuery pour performances
            var tasks = await _context.ScheduledTasks
                .AsNoTracking()
                .Where(t => t.CustomerId == customerId)
                .Include(t => t.User)
                .Include(t => t.ControlPoint)
                .AsSplitQuery()
                .OrderBy(t => t.ScheduledDate)
                .ToListAsync();
            return Ok(tasks);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ScheduledTask>> GetTask(Guid id)
        {
            var customerId = GetCustomerId();
            // Optimisation: AsNoTracking pour lecture seule + AsSplitQuery pour performances
            var task = await _context.ScheduledTasks
                .AsNoTracking()
                .Include(t => t.User)
                .Include(t => t.ControlPoint)
                .AsSplitQuery()
                .FirstOrDefaultAsync(t => t.Id == id && t.CustomerId == customerId);
            if (task == null)
                return NotFound();
            return Ok(task);
        }

        [HttpPost]
        public async Task<ActionResult<object>> CreateTask(ScheduledTask task)
        {
            var customerId = GetCustomerId();
            if (task.ScheduledDate.Kind == DateTimeKind.Unspecified)
            {
                task.ScheduledDate = DateTime.SpecifyKind(task.ScheduledDate, DateTimeKind.Utc);
            }
            if (task.ScheduledEndDate.HasValue && task.ScheduledEndDate.Value.Kind == DateTimeKind.Unspecified)
            {
                task.ScheduledEndDate = DateTime.SpecifyKind(task.ScheduledEndDate.Value, DateTimeKind.Utc);
            }
            if (!task.TaskTemplateId.HasValue)
            {
                task.TaskTemplateId = DEFAULT_TASK_TEMPLATE_ID;
            }
            var endOfYear = new DateTime(DateTime.UtcNow.Year, 12, 31, 23, 59, 59, DateTimeKind.Utc);
            var createdTasks = new List<ScheduledTask>();
            if (task.Recurrence == "ONCE")
            {
                task.Id = Guid.NewGuid();
                task.CustomerId = customerId;
                task.CreatedAt = DateTime.UtcNow;
                task.Status = "PENDING";
                _context.ScheduledTasks.Add(task);
                createdTasks.Add(task);
            }
            else
            {
                var currentDate = task.ScheduledDate;
                var taskDuration = task.ScheduledEndDate.HasValue
                    ? (task.ScheduledEndDate.Value - task.ScheduledDate).Days
                    : 0;
                while (currentDate <= endOfYear)
                {
                    var taskDate = currentDate;
                    if (task.WeekendHandling != "ALLOW")
                    {
                        var dayOfWeek = taskDate.DayOfWeek;
                        if (task.WeekendHandling == "SKIP")
                        {
                            if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
                            {
                                currentDate = task.Recurrence switch
                                {
                                    "DAILY" => currentDate.AddDays(1),
                                    "WEEKLY" => currentDate.AddDays(7),
                                    "MONTHLY" => currentDate.AddMonths(1),
                                    "QUARTERLY" => currentDate.AddMonths(3),
                                    "BIANNUAL" => currentDate.AddMonths(6),
                                    "YEARLY" => currentDate.AddYears(1),
                                    _ => currentDate.AddDays(1)
                                };
                                continue;
                            }
                        }
                        else if (task.WeekendHandling == "MOVE_TO_MONDAY")
                        {
                            if (dayOfWeek == DayOfWeek.Saturday)
                            {
                                taskDate = taskDate.AddDays(2);
                            }
                            else if (dayOfWeek == DayOfWeek.Sunday)
                            {
                                taskDate = taskDate.AddDays(1);
                            }
                        }
                    }
                    var newTask = new ScheduledTask
                    {
                        Id = Guid.NewGuid(),
                        CustomerId = customerId,
                        UserId = task.UserId,
                        ControlPointId = task.ControlPointId,
                        TaskTemplateId = task.TaskTemplateId,
                        ScheduledDate = taskDate,
                        ScheduledEndDate = taskDuration > 0 ? taskDate.AddDays(taskDuration) : null,
                        ScheduledTimeStart = task.ScheduledTimeStart,
                        ScheduledTimeEnd = task.ScheduledTimeEnd,
                        Recurrence = task.Recurrence,
                        WeekendHandling = task.WeekendHandling,
                        Status = "PENDING",
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.ScheduledTasks.Add(newTask);
                    createdTasks.Add(newTask);
                    currentDate = task.Recurrence switch
                    {
                        "DAILY" => currentDate.AddDays(1),
                        "WEEKLY" => currentDate.AddDays(7),
                        "MONTHLY" => currentDate.AddMonths(1),
                        "QUARTERLY" => currentDate.AddMonths(3),
                        "BIANNUAL" => currentDate.AddMonths(6),
                        "YEARLY" => currentDate.AddYears(1),
                        _ => currentDate.AddDays(1)
                    };
                }
            }
            await _context.SaveChangesAsync();
            return Ok(new
            {
                message = createdTasks.Count == 1
                    ? "Tâche créée avec succès"
                    : $"{createdTasks.Count} tâches récurrentes créées jusqu'au 31/12/{DateTime.UtcNow.Year}",
                taskCount = createdTasks.Count,
                firstTaskId = createdTasks.First().Id,
                recurrence = task.Recurrence
            });
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTask(Guid id, [FromBody] UpdateTaskRequest request)
        {
            var customerId = GetCustomerId();
            var existingTask = await _context.ScheduledTasks
                .Include(t => t.User)
                .Include(t => t.ControlPoint)
                .FirstOrDefaultAsync(t => t.Id == id && t.CustomerId == customerId);
            if (existingTask == null)
                return NotFound();
            var scheduledDate = request.ScheduledDate.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(request.ScheduledDate, DateTimeKind.Utc)
                : request.ScheduledDate;

            // Vérifier si c'est une nouvelle assignation (UserId change)
            var isNewAssignment = existingTask.UserId != request.UserId && request.UserId != Guid.Empty;
            var newUser = isNewAssignment ? await _context.Users.FindAsync(request.UserId) : null;

            if (request.ApplyToAllOccurrences && !string.IsNullOrEmpty(existingTask.Recurrence) && existingTask.Recurrence != "ONCE")
            {
                var allOccurrences = await _context.ScheduledTasks
                    .Where(t => t.CustomerId == customerId &&
                               t.ControlPointId == existingTask.ControlPointId &&
                               t.Recurrence == existingTask.Recurrence &&
                               t.ScheduledDate >= existingTask.ScheduledDate &&
                               t.Status == "PENDING")
                    .ToListAsync();
                foreach (var occurrence in allOccurrences)
                {
                    occurrence.UserId = request.UserId;
                    occurrence.ScheduledTimeStart = request.ScheduledTimeStart;
                    occurrence.ScheduledTimeEnd = request.ScheduledTimeEnd;
                }
                await _context.SaveChangesAsync();

                // Envoyer l'email d'assignation si c'est une nouvelle assignation
                if (isNewAssignment && newUser != null)
                {
                    try
                    {
                        var taskName = existingTask.ControlPoint?.Name ?? "Tâche sans nom";
                        var emailSent = await _emailService.SendTaskAssignmentEmailAsync(
                            newUser.Email,
                            newUser.Prenom,
                            taskName,
                            $"Vous avez été assigné à {allOccurrences.Count} tâche(s) récurrente(s)",
                            existingTask.ScheduledDate
                        );
                        if (emailSent)
                        {
                            _logger.LogInformation($"[EMAIL] Email d'assignation de tâche envoyé à {newUser.Email}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"[EMAIL] Erreur lors de l'envoi de l'email d'assignation à {newUser.Email}");
                    }
                }

                return Ok(new { message = $"{allOccurrences.Count} tâche(s) récurrente(s) modifiée(s) avec succès" });
            }
            else
            {
                existingTask.ScheduledDate = scheduledDate;
                existingTask.UserId = request.UserId;
                existingTask.ScheduledTimeStart = request.ScheduledTimeStart;
                existingTask.ScheduledTimeEnd = request.ScheduledTimeEnd;
                await _context.SaveChangesAsync();

                // Envoyer l'email d'assignation si c'est une nouvelle assignation
                if (isNewAssignment && newUser != null)
                {
                    try
                    {
                        var taskName = existingTask.ControlPoint?.Name ?? "Tâche sans nom";
                        var emailSent = await _emailService.SendTaskAssignmentEmailAsync(
                            newUser.Email,
                            newUser.Prenom,
                            taskName,
                            "Vous avez été assigné à une nouvelle tâche",
                            existingTask.ScheduledDate
                        );
                        if (emailSent)
                        {
                            _logger.LogInformation($"[EMAIL] Email d'assignation de tâche envoyé à {newUser.Email}");
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"[EMAIL] Erreur lors de l'envoi de l'email d'assignation à {newUser.Email}");
                    }
                }

                return Ok(new { message = "Tâche modifiée avec succès" });
            }
        }

        public class UpdateTaskRequest
        {
            public Guid UserId { get; set; }
            public DateTime ScheduledDate { get; set; }
            public TimeSpan? ScheduledTimeStart { get; set; }
            public TimeSpan? ScheduledTimeEnd { get; set; }
            public bool ApplyToAllOccurrences { get; set; } = false;
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTask(Guid id)
        {
            var customerId = GetCustomerId();
            var task = await _context.ScheduledTasks
                .FirstOrDefaultAsync(t => t.Id == id && t.CustomerId == customerId);
            if (task == null)
                return NotFound();
            _context.ScheduledTasks.Remove(task);
            await _context.SaveChangesAsync();
            return NoContent();
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<ScheduledTask>>> GetTasksByUser(Guid userId)
        {
            var customerId = GetCustomerId();
            var tasks = await _context.ScheduledTasks
                .Where(t => t.CustomerId == customerId && t.UserId == userId)
                .Include(t => t.User)
                .Include(t => t.ControlPoint)
                .OrderBy(t => t.ScheduledDate)
                .ThenBy(t => t.ScheduledTimeStart)
                .ToListAsync();
            return Ok(tasks);
        }

        [HttpGet("calendar")]
        public async Task<ActionResult<IEnumerable<ScheduledTask>>> GetTasksByDateRange([FromQuery] DateTime start, [FromQuery] DateTime end)
        {
            var customerId = GetCustomerId();
            if (start.Kind == DateTimeKind.Unspecified)
                start = DateTime.SpecifyKind(start, DateTimeKind.Utc);
            if (end.Kind == DateTimeKind.Unspecified)
                end = DateTime.SpecifyKind(end, DateTimeKind.Utc);
            var tasks = await _context.ScheduledTasks
                .Where(t => t.CustomerId == customerId && t.ScheduledDate >= start && t.ScheduledDate <= end)
                .Include(t => t.User)
                .Include(t => t.ControlPoint)
                    .ThenInclude(cp => cp.Zone)
                .Include(t => t.ControlPoint)
                    .ThenInclude(cp => cp.Asset)
                        .ThenInclude(a => a.Zone)
                .Include(t => t.TaskTemplate)
                .OrderBy(t => t.ScheduledDate)
                .ThenBy(t => t.ScheduledTimeStart)
                .Select(t => new
                {
                    t.Id,
                    t.ControlPointId,
                    t.UserId,
                    t.ScheduledDate,
                    t.ScheduledEndDate,
                    t.ScheduledTimeStart,
                    t.ScheduledTimeEnd,
                    t.Status,
                    t.Recurrence,
                    t.TaskTemplateId,
                    User = t.User != null ? new { t.User.Id, t.User.Prenom, t.User.Nom, t.User.Email, t.User.Role } : null,
                    ControlPoint = t.ControlPoint != null ? new
                    {
                        t.ControlPoint.Id,
                        t.ControlPoint.Name,
                        t.ControlPoint.Code,
                        Zone = t.ControlPoint.Zone != null ? new { t.ControlPoint.Zone.Id, t.ControlPoint.Zone.Name, t.ControlPoint.Zone.SiteId } : null,
                        Asset = t.ControlPoint.Asset != null ? new { t.ControlPoint.Asset.Id, t.ControlPoint.Asset.Name, Zone = t.ControlPoint.Asset.Zone != null ? new { t.ControlPoint.Asset.Zone.Id, t.ControlPoint.Asset.Zone.Name, t.ControlPoint.Asset.Zone.SiteId } : null } : null
                    } : null,
                    TaskTemplate = t.TaskTemplate != null ? new { t.TaskTemplate.Id, t.TaskTemplate.Name } : null
                })
                .ToListAsync();
            return Ok(tasks);
        }

        [HttpGet("overdue")]
        public async Task<ActionResult<IEnumerable<ScheduledTask>>> GetOverdueTasks()
        {
            var customerId = GetCustomerId();
            var now = DateTime.UtcNow;
            var tasks = await _context.ScheduledTasks
                .Where(t => t.CustomerId == customerId && t.Status != "COMPLETED" && t.Status != "CANCELLED" && t.ScheduledDate < now)
                .Include(t => t.User)
                .Include(t => t.ControlPoint)
                .Include(t => t.ControlPoint)
                    .ThenInclude(cp => cp.Asset)
                .OrderBy(t => t.ScheduledDate)
                .ToListAsync();
            return Ok(tasks);
        }

        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetTaskStats([FromQuery] DateTime? date)
        {
            var customerId = GetCustomerId();
            var targetDate = date?.Date ?? DateTime.UtcNow.Date;
            var allTasks = await _context.ScheduledTasks
                .Where(t => t.CustomerId == customerId)
                .ToListAsync();
            var todayTasks = allTasks.Where(t => t.ScheduledDate.Date == targetDate).ToList();
            var stats = new
            {
                Date = targetDate,
                Total = todayTasks.Count,
                Pending = todayTasks.Count(t => t.Status == "PENDING"),
                InProgress = todayTasks.Count(t => t.Status == "IN_PROGRESS"),
                Completed = todayTasks.Count(t => t.Status == "COMPLETED"),
                Overdue = allTasks.Count(t => t.Status != "COMPLETED" && t.Status != "CANCELLED" && t.ScheduledDate < targetDate),
                Cancelled = todayTasks.Count(t => t.Status == "CANCELLED")
            };
            return Ok(stats);
        }

        [HttpGet("today")]
        public async Task<ActionResult<IEnumerable<ScheduledTask>>> GetTodayTasks()
        {
            var customerId = GetCustomerId();
            var today = DateTime.Now.Date;
            var tomorrow = today.AddDays(1);
            var todayUtc = DateTime.SpecifyKind(today, DateTimeKind.Utc);
            var tomorrowUtc = DateTime.SpecifyKind(tomorrow, DateTimeKind.Utc);
            var tasks = await _context.ScheduledTasks
                .Where(t => t.CustomerId == customerId && t.ScheduledDate >= todayUtc && t.ScheduledDate < tomorrowUtc)
                .Include(t => t.User)
                .Include(t => t.ControlPoint)
                    .ThenInclude(cp => cp.Zone)
                .Include(t => t.ControlPoint)
                    .ThenInclude(cp => cp.Asset)
                        .ThenInclude(a => a.Zone)
                .Include(t => t.TaskTemplate)
                .OrderBy(t => t.ScheduledTimeStart)
                .Select(t => new
                {
                    t.Id,
                    t.ControlPointId,
                    t.UserId,
                    t.ScheduledDate,
                    t.ScheduledTimeStart,
                    t.ScheduledTimeEnd,
                    t.Status,
                    t.Recurrence,
                    t.TaskTemplateId,
                    t.RequireDoubleScan,
                    User = new { t.User.Id, t.User.Prenom, t.User.Nom, t.User.Email },
                    ControlPoint = new
                    {
                        t.ControlPoint.Id,
                        t.ControlPoint.Name,
                        t.ControlPoint.Code,
                        SiteId = t.ControlPoint.Zone != null ? t.ControlPoint.Zone.SiteId : (t.ControlPoint.Asset != null && t.ControlPoint.Asset.Zone != null ? t.ControlPoint.Asset.Zone.SiteId : (Guid?)null),
                        Zone = t.ControlPoint.Zone != null ? new { t.ControlPoint.Zone.Id, t.ControlPoint.Zone.Name, t.ControlPoint.Zone.SiteId } : null
                    },
                    TaskTemplate = t.TaskTemplate != null ? new { t.TaskTemplate.Id, t.TaskTemplate.Name } : null
                })
                .ToListAsync();
            return Ok(tasks);
        }

        [HttpPost("check-availability")]
        public async Task<ActionResult<object>> CheckAvailability([FromBody] CheckAvailabilityRequest request)
        {
            var customerId = GetCustomerId();
            var startDate = request.ScheduledDate.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(request.ScheduledDate, DateTimeKind.Utc)
                : request.ScheduledDate;
            var endDate = request.ScheduledEndDate.HasValue
                ? (request.ScheduledEndDate.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(request.ScheduledEndDate.Value, DateTimeKind.Utc)
                    : request.ScheduledEndDate.Value)
                : startDate;
            var query = _context.ScheduledTasks
                .Where(t => t.CustomerId == customerId && t.UserId == request.UserId && t.Status != "CANCELLED"
                    && ((t.ScheduledEndDate.HasValue ? t.ScheduledEndDate.Value : t.ScheduledDate) >= startDate && t.ScheduledDate <= endDate));
            if (request.ExcludeTaskId.HasValue)
            {
                query = query.Where(t => t.Id != request.ExcludeTaskId.Value);
            }
            var existingTasks = await query
                .Include(t => t.ControlPoint)
                    .ThenInclude(cp => cp.Zone)
                .Include(t => t.ControlPoint)
                    .ThenInclude(cp => cp.Asset)
                        .ThenInclude(a => a.Zone)
                .OrderBy(t => t.ScheduledDate)
                .ThenBy(t => t.ScheduledTimeStart)
                .ToListAsync();
            var conflicts = new List<object>();
            foreach (var existingTask in existingTasks)
            {
                if (!request.ScheduledTimeStart.HasValue || !request.ScheduledTimeEnd.HasValue
                    || !existingTask.ScheduledTimeStart.HasValue || !existingTask.ScheduledTimeEnd.HasValue)
                {
                    conflicts.Add(new { existingTask.Id, existingTask.ScheduledDate, existingTask.ScheduledEndDate, existingTask.ScheduledTimeStart, existingTask.ScheduledTimeEnd, existingTask.Status, Reason = "Horaire non défini" });
                    continue;
                }
                var taskStartDate = existingTask.ScheduledDate.Date;
                var taskEndDate = (existingTask.ScheduledEndDate ?? existingTask.ScheduledDate).Date;
                var newTaskStartDate = startDate.Date;
                var newTaskEndDate = endDate.Date;
                if (taskEndDate >= newTaskStartDate && taskStartDate <= newTaskEndDate)
                {
                    var existingStart = existingTask.ScheduledTimeStart.Value;
                    var existingEnd = existingTask.ScheduledTimeEnd.Value;
                    var newStart = request.ScheduledTimeStart.Value;
                    var newEnd = request.ScheduledTimeEnd.Value;
                    if (newStart < existingEnd && newEnd > existingStart)
                    {
                        conflicts.Add(new { existingTask.Id, existingTask.ScheduledDate, Reason = $"Chevauchement horaire: {existingStart:hh\\:mm} - {existingEnd:hh\\:mm}" });
                    }
                }
            }
            return Ok(new
            {
                IsAvailable = conflicts.Count == 0,
                ConflictCount = conflicts.Count,
                Conflicts = conflicts
            });
        }

        public class CheckAvailabilityRequest
        {
            public Guid UserId { get; set; }
            public DateTime ScheduledDate { get; set; }
            public DateTime? ScheduledEndDate { get; set; }
            public TimeSpan? ScheduledTimeStart { get; set; }
            public TimeSpan? ScheduledTimeEnd { get; set; }
            public Guid? ExcludeTaskId { get; set; }
        }

        [HttpPost("{id}/cancel")]
        public async Task<ActionResult> CancelTask(Guid id, [FromBody] CancelTaskRequest request)
        {
            var customerId = GetCustomerId();
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim))
            {
                return Unauthorized(new { error = "Utilisateur non identifié" });
            }
            var userId = Guid.Parse(userIdClaim);
            if (string.IsNullOrWhiteSpace(request.CancellationReason) || request.CancellationReason.Length < 10)
            {
                return BadRequest(new { error = "Le motif d'annulation est obligatoire (min. 10 caractères)" });
            }
            var task = await _context.ScheduledTasks
                .Where(t => t.Id == id && t.CustomerId == customerId)
                .FirstOrDefaultAsync();
            if (task == null)
            {
                return NotFound(new { error = "Tâche non trouvée" });
            }
            if (task.Status == "COMPLETED" || task.Status == "CANCELLED")
            {
                return BadRequest(new { error = "Impossible d'annuler cette tâche" });
            }
            int cancelledCount = 0;
            if (request.ApplyToAllOccurrences && !string.IsNullOrEmpty(task.Recurrence) && task.Recurrence != "ONCE")
            {
                var allOccurrences = await _context.ScheduledTasks
                    .Where(t => t.CustomerId == customerId && t.ControlPointId == task.ControlPointId
                        && t.Recurrence == task.Recurrence && t.ScheduledDate >= task.ScheduledDate
                        && t.Status != "COMPLETED" && t.Status != "CANCELLED")
                    .ToListAsync();
                foreach (var occurrence in allOccurrences)
                {
                    occurrence.Status = "CANCELLED";
                    occurrence.CancellationReason = request.CancellationReason;
                    occurrence.CancelledAt = DateTime.UtcNow;
                    occurrence.CancelledBy = userId;
                }
                cancelledCount = allOccurrences.Count;
                await _context.SaveChangesAsync();
                return Ok(new { message = $"{cancelledCount} tâche(s) annulée(s)", cancelledCount });
            }
            else
            {
                task.Status = "CANCELLED";
                task.CancellationReason = request.CancellationReason;
                task.CancelledAt = DateTime.UtcNow;
                task.CancelledBy = userId;
                await _context.SaveChangesAsync();
                return Ok(new { message = "Tâche annulée avec succès" });
            }
        }

        public class CancelTaskRequest
        {
            public string CancellationReason { get; set; } = string.Empty;
            public bool ApplyToAllOccurrences { get; set; } = false;
        }
    }
}
