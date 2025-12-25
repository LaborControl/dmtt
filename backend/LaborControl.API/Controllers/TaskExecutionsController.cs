using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaborControl.API.Data;
using LaborControl.API.Models;

namespace LaborControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TaskExecutionsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TaskExecutionsController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ========================================
        // ENDPOINT DE TEST - Vérifier version déployée
        // ========================================
        [HttpGet("version")]
        public IActionResult GetVersion()
        {
            return Ok(new { 
                version = "2.0-WITH-INCLUDE", 
                timestamp = DateTime.UtcNow,
                message = "Version avec .Include() pour Customer et ControlPoint"
            });
        }

        [HttpGet("{userId:guid}")]
        public async Task<ActionResult<IEnumerable<object>>> GetUserExecutions(
            Guid userId, 
            [FromQuery] DateTime? startDate = null)
        {
            try
            {
                Console.WriteLine($"[DEBUG] GetUserExecutions - UserId: {userId}, StartDate: {startDate}");
                
                // ✅ IMPORTANT : .Include() pour charger les relations AVANT le .Select()
                // Évite les erreurs 500 si les relations sont nulles
                var query = _context.TaskExecutions
                    .Include(te => te.ControlPoint)
                    .Include(te => te.Customer)
                    .Where(te => te.UserId == userId);
                
                Console.WriteLine($"[DEBUG] Query créée avec Include");
                
                // Appliquer le filtre de date si fourni
                if (startDate.HasValue)
                {
                    query = query.Where(te => te.ScannedAt >= startDate.Value);
                    Console.WriteLine($"[DEBUG] Filtre de date appliqué: {startDate.Value}");
                }
                
                Console.WriteLine($"[DEBUG] Exécution de la requête...");
                
                var executions = await query
                    .OrderByDescending(te => te.ScannedAt)
                    .Select(te => new
                    {
                        te.Id,
                        te.ScannedAt,
                        te.SubmittedAt,
                        ControlPointName = te.ControlPoint != null ? te.ControlPoint.Name : "Point inconnu",
                        CustomerName = te.Customer != null ? te.Customer.Name : "Client inconnu",
                        Type = te.ScheduledTaskId.HasValue ? "SCHEDULED" : "UNSCHEDULED",
                        Status = "COMPLETED",
                        FormDataJson = te.FormData ?? "{}",
                        te.FlagSuspiciousValue,
                        te.FlagQuickEntry,
                        te.FlagOutOfRange
                    })
                    .ToListAsync();
                    
                Console.WriteLine($"[DEBUG] Requête terminée - {executions.Count} résultats");
                    
                return Ok(executions);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] Exception dans GetUserExecutions: {ex.Message}");
                Console.WriteLine($"[ERROR] StackTrace: {ex.StackTrace}");
                Console.WriteLine($"[ERROR] InnerException: {ex.InnerException?.Message}");
                
                return StatusCode(500, new { 
                    error = "Erreur serveur",
                    message = ex.Message,
                    innerMessage = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpGet("user/{userId}/today")]
        public async Task<ActionResult<IEnumerable<object>>> GetTodayExecutions(Guid userId)
        {
            var today = DateTime.UtcNow.Date;
            
            // ✅ .Include() pour charger les relations
            var executions = await _context.TaskExecutions
                .Include(te => te.ControlPoint)
                .Include(te => te.Customer)
                .Where(te => te.UserId == userId && te.ScannedAt.Date == today)
                .Select(te => new
                {
                    te.Id,
                    te.ScannedAt,
                    te.SubmittedAt,
                    ControlPointName = te.ControlPoint!.Name,
                    CustomerName = te.Customer!.Name,
                    te.FormData,
                    te.FlagSuspiciousValue,
                    te.FlagQuickEntry,
                    te.FlagOutOfRange
                })
                .ToListAsync();
                
            return Ok(executions);
        }

        [HttpGet("{executionId}/detail")]
        public async Task<ActionResult<object>> GetExecutionDetail(Guid executionId)
        {
            // ✅ .Include() pour charger les relations puis Select() pour éviter les cycles
            var execution = await _context.TaskExecutions
                .Include(te => te.ControlPoint)
                .Include(te => te.User)
                .Include(te => te.Customer)
                .Where(te => te.Id == executionId)
                .Select(te => new
                {
                    te.Id,
                    te.ScannedAt,
                    te.SubmittedAt,
                    te.FormData,
                    te.PhotoUrl,
                    ControlPoint = te.ControlPoint != null ? new
                    {
                        te.ControlPoint.Id,
                        te.ControlPoint.Name,
                        te.ControlPoint.LocationDescription
                    } : null,
                    User = te.User != null ? new
                    {
                        te.User.Id,
                        te.User.Email,
                        te.User.JobTitle,
                        te.User.Role
                    } : null,
                    Customer = te.Customer != null ? new
                    {
                        te.Customer.Id,
                        te.Customer.Name
                    } : null
                })
                .FirstOrDefaultAsync();
                
            if (execution == null)
            {
                return NotFound();
            }
            
            return Ok(execution);
        }

        [HttpPost]
        public async Task<ActionResult<TaskExecution>> CreateExecution([FromBody] CreateExecutionRequest request)
        {
            // Vérifier que le ControlPoint existe
            var controlPoint = await _context.ControlPoints
                .FirstOrDefaultAsync(cp => cp.Id == request.ControlPointId);
                
            if (controlPoint == null)
            {
                return BadRequest("Point de contrôle invalide");
            }
            
            // Vérifier que l'utilisateur existe
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId);
                
            if (user == null)
            {
                return BadRequest("Utilisateur invalide");
            }
            
            var execution = new TaskExecution
            {
                Id = Guid.NewGuid(),
                ScheduledTaskId = request.ScheduledTaskId,
                ControlPointId = request.ControlPointId,
                UserId = request.UserId,
                CustomerId = user.CustomerId,
                ScannedAt = request.ScannedAt,
                SubmittedAt = request.SubmittedAt,
                FormData = request.FormData ?? "{}",
                PhotoUrl = request.PhotoUrl
            };
            
            // Détection anti-triche
            
            // 1. Détection saisie différée
            if (execution.SubmittedAt.HasValue)
            {
                var delai = execution.SubmittedAt.Value - execution.ScannedAt;
                if (delai.TotalMinutes > 30)
                {
                    execution.FlagSaisieDifferee = true;
                }
                
                // 2. Détection saisie trop rapide
                if (delai.TotalSeconds < 5)
                {
                    execution.FlagSaisieRapide = true;
                }
            }
            
            // 3. Détection valeurs répétées (simplifiée pour le moment)
            // TODO: Comparer avec les 3 dernières valeurs
            
            _context.TaskExecutions.Add(execution);
            await _context.SaveChangesAsync();
            
            // Mettre à jour le statut de la tâche planifiée si elle existe
            if (request.ScheduledTaskId.HasValue)
            {
                var scheduledTask = await _context.ScheduledTasks
                    .FirstOrDefaultAsync(st => st.Id == request.ScheduledTaskId.Value);
                    
                if (scheduledTask != null)
                {
                    scheduledTask.Status = "COMPLETED";
                    scheduledTask.TaskExecutionId = execution.Id;
                    await _context.SaveChangesAsync();
                }
            }
            
            // Retourner l'exécution avec un résumé des flags
            return Ok(new
            {
                execution.Id,
                execution.ScannedAt,
                execution.SubmittedAt,
                Alerts = new
                {
                    ValeurSuspecte = execution.FlagValeurRepetee || execution.FlagSaisieRapide,
                    HorsLimites = execution.FlagHorsMarge || execution.FlagEcartOcr || execution.FlagSaisieDifferee
                }
            });
        }

        // ========================================
        // DOUBLE BORNAGE - PREMIER SCAN
        // ========================================
        [HttpPost("first-scan")]
        public async Task<ActionResult<object>> FirstScan([FromBody] FirstScanRequest request)
        {
            // Vérifier que le ControlPoint existe
            var controlPoint = await _context.ControlPoints
                .FirstOrDefaultAsync(cp => cp.Id == request.ControlPointId);

            if (controlPoint == null)
            {
                return BadRequest("Point de contrôle invalide");
            }

            // Vérifier que l'utilisateur existe
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId);

            if (user == null)
            {
                return BadRequest("Utilisateur invalide");
            }

            // Vérifier que la tâche planifiée existe et nécessite double scan
            ScheduledTask? scheduledTask = null;
            if (request.ScheduledTaskId.HasValue)
            {
                scheduledTask = await _context.ScheduledTasks
                    .FirstOrDefaultAsync(st => st.Id == request.ScheduledTaskId.Value);

                if (scheduledTask == null)
                {
                    return BadRequest("Tâche planifiée invalide");
                }

                if (!scheduledTask.RequireDoubleScan)
                {
                    return BadRequest("Cette tâche ne nécessite pas de double bornage");
                }

                // Vérifier que la tâche n'est pas déjà en cours
                if (scheduledTask.Status != "PENDING")
                {
                    return BadRequest($"Cette tâche est déjà en statut {scheduledTask.Status}");
                }
            }

            // Créer l'exécution avec premier scan seulement
            var execution = new TaskExecution
            {
                Id = Guid.NewGuid(),
                ScheduledTaskId = request.ScheduledTaskId,
                ControlPointId = request.ControlPointId,
                UserId = request.UserId,
                CustomerId = user.CustomerId,
                ScannedAt = request.FirstScanAt,
                FirstScanAt = request.FirstScanAt,
                SecondScanAt = null, // Pas encore fait
                SubmittedAt = null,   // Pas encore soumis
                FormData = "{}"       // Formulaire vide
            };

            _context.TaskExecutions.Add(execution);

            // Mettre à jour le statut de la tâche planifiée à IN_PROGRESS
            if (scheduledTask != null)
            {
                scheduledTask.Status = "IN_PROGRESS";
                scheduledTask.TaskExecutionId = execution.Id;
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                ExecutionId = execution.Id,
                Status = "FIRST_SCAN_COMPLETED",
                Message = "Premier scan effectué. Veuillez remplir le formulaire puis scanner à nouveau pour valider.",
                FirstScanAt = execution.FirstScanAt,
                RequireSecondScan = true
            });
        }

        // ========================================
        // DOUBLE BORNAGE - SECOND SCAN
        // ========================================
        [HttpPost("second-scan")]
        public async Task<ActionResult<object>> SecondScan([FromBody] SecondScanRequest request)
        {
            // Récupérer l'exécution existante
            var execution = await _context.TaskExecutions
                .Include(te => te.ScheduledTask)
                .FirstOrDefaultAsync(te => te.Id == request.ExecutionId);

            if (execution == null)
            {
                return BadRequest("Exécution introuvable");
            }

            // Vérifier que le premier scan a été fait
            if (!execution.FirstScanAt.HasValue)
            {
                return BadRequest("Le premier scan n'a pas été effectué");
            }

            // Vérifier que le second scan n'a pas déjà été fait
            if (execution.SecondScanAt.HasValue)
            {
                return BadRequest("Le second scan a déjà été effectué");
            }

            // Vérifier le temps minimum entre les deux scans (minimum 30 secondes)
            var timeBetweenScans = request.SecondScanAt - execution.FirstScanAt.Value;
            if (timeBetweenScans.TotalSeconds < 30)
            {
                return BadRequest("Temps insuffisant entre les deux scans. Minimum 30 secondes requis.");
            }

            // Mettre à jour l'exécution avec le second scan
            execution.SecondScanAt = request.SecondScanAt;
            execution.SubmittedAt = request.SecondScanAt;
            execution.FormData = request.FormData ?? "{}";
            execution.PhotoUrl = request.PhotoUrl;

            // Détection anti-triche améliorée pour le double bornage
            var totalWorkTime = execution.SecondScanAt.Value - execution.FirstScanAt.Value;

            // Détection saisie trop rapide (moins de 1 minute de travail total)
            if (totalWorkTime.TotalMinutes < 1)
            {
                execution.FlagSaisieRapide = true;
            }

            // Détection saisie différée (plus de 2 heures de travail total)
            if (totalWorkTime.TotalHours > 2)
            {
                execution.FlagSaisieDifferee = true;
            }

            // Mettre à jour le statut de la tâche planifiée à COMPLETED
            if (execution.ScheduledTask != null)
            {
                execution.ScheduledTask.Status = "COMPLETED";
            }

            await _context.SaveChangesAsync();

            return Ok(new
            {
                ExecutionId = execution.Id,
                Status = "DOUBLE_SCAN_COMPLETED",
                Message = "Tâche validée avec succès via double bornage.",
                FirstScanAt = execution.FirstScanAt,
                SecondScanAt = execution.SecondScanAt,
                TotalWorkTime = $"{totalWorkTime.TotalMinutes:F1} minutes",
                Alerts = new
                {
                    TravailTropRapide = execution.FlagSaisieRapide,
                    TravailTropLong = execution.FlagSaisieDifferee
                }
            });
        }
    }
    
    public class CreateExecutionRequest
    {
        public Guid? ScheduledTaskId { get; set; }
        public Guid ControlPointId { get; set; }
        public Guid UserId { get; set; }
        public DateTime ScannedAt { get; set; }
        public DateTime? SubmittedAt { get; set; }
        public string? FormData { get; set; }
        public string? PhotoUrl { get; set; }
    }

    public class FirstScanRequest
    {
        public Guid? ScheduledTaskId { get; set; }
        public Guid ControlPointId { get; set; }
        public Guid UserId { get; set; }
        public DateTime FirstScanAt { get; set; }
    }

    public class SecondScanRequest
    {
        public Guid ExecutionId { get; set; }
        public DateTime SecondScanAt { get; set; }
        public string? FormData { get; set; }
        public string? PhotoUrl { get; set; }
    }
}