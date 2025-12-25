using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaborControl.API.Data;
using LaborControl.API.Models;
using LaborControl.API.DTOs;

namespace LaborControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ScheduledTasksController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public ScheduledTasksController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetUserTasks(Guid userId)
        {
            var tasks = await _context.ScheduledTasks
                .Where(st => st.UserId == userId && st.Status == "PENDING")
                .Include(st => st.ControlPoint)
                    .ThenInclude(cp => cp!.Asset)
                .Include(st => st.ControlPoint)
                    .ThenInclude(cp => cp!.RfidChip)
                .Include(st => st.TaskTemplate)
                .OrderBy(st => st.ScheduledDate)
                .ThenBy(st => st.ScheduledTimeStart)
                .Select(st => new
                {
                    id = st.Id,
                    scheduledDate = st.ScheduledDate,
                    scheduledTimeStart = st.ScheduledTimeStart,
                    scheduledTimeEnd = st.ScheduledTimeEnd,
                    status = st.Status,
                    recurrence = st.Recurrence,
                    controlPoint = st.ControlPoint == null ? null : new
                    {
                        id = st.ControlPoint.Id,
                        name = st.ControlPoint.Name,
                        locationDescription = st.ControlPoint.LocationDescription,
                        rfidChip = st.ControlPoint.RfidChip == null ? null : new
                        {
                            chipId = st.ControlPoint.RfidChip.ChipId
                        }
                    }
                })
                .ToListAsync();

            return Ok(tasks);
        }

        [HttpPost]
        public async Task<ActionResult<ScheduledTask>> CreateTask(ScheduledTask task)
        {
            task.Id = Guid.NewGuid();
            task.CreatedAt = DateTime.UtcNow;
            _context.ScheduledTasks.Add(task);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTask), new { id = task.Id }, task);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ScheduledTask>> GetTask(Guid id)
        {
            var task = await _context.ScheduledTasks
                .Include(st => st.ControlPoint)
                .Include(st => st.User)
                .Include(st => st.TaskTemplate)
                .FirstOrDefaultAsync(st => st.Id == id);

            if (task == null)
            {
                return NotFound();
            }

            return task;
        }

        /// <summary>
        /// Récupère le nombre total de tâches planifiées de tous les clients (passées + futures)
        /// </summary>
        [HttpGet("count/all")]
        public async Task<ActionResult<int>> GetAllScheduledTasksCount()
        {
            var count = await _context.ScheduledTasks
                .CountAsync();

            return Ok(count);
        }

        /// <summary>
        /// Récupère les tendances du nombre de tâches planifiées (pour dashboard)
        /// </summary>
        [HttpGet("count/trend")]
        public async Task<ActionResult<TrendResponse>> GetScheduledTasksCountTrend()
        {
            var now = DateTime.UtcNow;
            var firstDayThisMonth = new DateTime(now.Year, now.Month, 1);
            var firstDayLastMonth = firstDayThisMonth.AddMonths(-1);

            var total = await _context.ScheduledTasks.CountAsync();

            var thisMonth = await _context.ScheduledTasks
                .Where(t => t.CreatedAt >= firstDayThisMonth)
                .CountAsync();

            var lastMonth = await _context.ScheduledTasks
                .Where(t => t.CreatedAt >= firstDayLastMonth && t.CreatedAt < firstDayThisMonth)
                .CountAsync();

            var percentChange = lastMonth > 0
                ? ((thisMonth - lastMonth) / (double)lastMonth) * 100
                : 0;

            return Ok(new TrendResponse
            {
                Total = total,
                ThisMonth = thisMonth,
                LastMonth = lastMonth,
                PercentChange = Math.Round(percentChange, 1)
            });
        }
    }
}
