using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaborControl.API.Data;
using LaborControl.API.Models;

namespace LaborControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TestController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("create-test-task")]
        public async Task<ActionResult> CreateTestTask()
        {
            // Récupérer le premier user
            var user = await _context.Users.FirstOrDefaultAsync();
            if (user == null) return BadRequest("Aucun utilisateur trouvé");

            // Récupérer le premier ControlPoint avec Asset
            var controlPoint = await _context.ControlPoints
                .Include(cp => cp.Asset)
                .FirstOrDefaultAsync();
            if (controlPoint == null) return BadRequest("Aucun ControlPoint trouvé");

            // Récupérer un template EHPAD
            var template = await _context.TaskTemplates
                .FirstOrDefaultAsync(t => t.Category == "SURVEILLANCE");
            
            // Créer une tâche planifiée pour aujourd'hui
            var task = new ScheduledTask
            {
                Id = Guid.NewGuid(),
                CustomerId = user.CustomerId,
                UserId = user.Id,
                ControlPointId = controlPoint.Id,
                TaskTemplateId = template?.Id,
                ScheduledDate = DateTime.UtcNow.Date,
                ScheduledTimeStart = new TimeSpan(8, 0, 0),
                ScheduledTimeEnd = new TimeSpan(12, 0, 0),
                Status = "PENDING",
                CreatedAt = DateTime.UtcNow
            };

            _context.ScheduledTasks.Add(task);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                message = "Tâche de test créée",
                task = new
                {
                    Id = task.Id,
                    User = user.Email,
                    ControlPoint = controlPoint.Name,
                    Asset = controlPoint.Asset?.Name,
                    Template = template?.Name,
                    Schedule = $"{task.ScheduledDate:dd/MM/yyyy} {task.ScheduledTimeStart}-{task.ScheduledTimeEnd}"
                }
            });
        }

        [HttpPost("apply-migrations")]
        public async Task<ActionResult> ApplyMigrations([FromHeader(Name = "X-Admin-Key")] string? adminKey)
        {
            // Sécurité: vérifier la clé d'administration
            if (adminKey != "LaborControl2025!Migration")
            {
                return Unauthorized(new { message = "Clé d'administration invalide" });
            }

            try
            {
                // Récupérer les migrations en attente
                var pendingMigrations = await _context.Database.GetPendingMigrationsAsync();
                var pendingList = pendingMigrations.ToList();

                if (!pendingList.Any())
                {
                    return Ok(new { message = "Aucune migration en attente", appliedMigrations = new string[0] });
                }

                // Appliquer les migrations
                await _context.Database.MigrateAsync();

                return Ok(new
                {
                    message = "Migrations appliquées avec succès",
                    appliedMigrations = pendingList
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Erreur lors de l'application des migrations", error = ex.Message });
            }
        }

        [HttpGet("architecture-summary")]
        public async Task<ActionResult> GetArchitectureSummary()
        {
            var customer = await _context.Customers
                .Include(c => c.Sites)
                    .ThenInclude(s => s.Zones)
                        .ThenInclude(z => z.Assets)
                            .ThenInclude(a => a.ControlPoints)
                .FirstOrDefaultAsync();

            if (customer == null) return NotFound("Aucun customer");

            var summary = new
            {
                Customer = new
                {
                    customer.Name,
                    // TODO: Sector supprimé du modèle Customer - maintenant au niveau des Teams
                    // Sector = customer.Sector,
                    customer.IsMultiSite,
                    Sites = customer.Sites.Select(s => new
                    {
                        s.Name,
                        s.Code,
                        Zones = s.Zones.Select(z => new
                        {
                            z.Name,
                            z.Type,
                            Assets = z.Assets.Select(a => new
                            {
                                a.Name,
                                a.Type,
                                a.Status,
                                ControlPoints = a.ControlPoints.Select(cp => new
                                {
                                    cp.Name,
                                    HasRfidChip = cp.RfidChipId != null
                                })
                            })
                        })
                    })
                }
            };

            return Ok(summary);
        }
    }
}