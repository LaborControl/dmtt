using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaborControl.API.Data;
using LaborControl.API.Models;

namespace LaborControl.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EquipmentStatusesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EquipmentStatusesController(ApplicationDbContext context)
        {
            _context = context;
        }

        private Guid GetCustomerId()
        {
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;
            if (string.IsNullOrEmpty(customerIdClaim) || !Guid.TryParse(customerIdClaim, out var customerId))
            {
                throw new UnauthorizedAccessException("Customer ID not found in token");
            }
            return customerId;
        }

        // GET: api/equipmentstatuses
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EquipmentStatusDto>>> GetStatuses()
        {
            var customerId = GetCustomerId();

            var statuses = await _context.EquipmentStatuses
                .Where(s => s.CustomerId == customerId && s.IsActive)
                .OrderBy(s => s.DisplayOrder)
                .ThenBy(s => s.Name)
                .ToListAsync();

            var dtos = statuses.Select(s => new EquipmentStatusDto
            {
                Id = s.Id,
                Name = s.Name,
                Code = s.Code,
                Description = s.Description,
                Color = s.Color,
                Icon = s.Icon,
                DisplayOrder = s.DisplayOrder,
                IsActive = s.IsActive,
                IsPredefined = s.IsPredefined
            }).ToList();

            return Ok(dtos);
        }

        // GET: api/equipmentstatuses/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<EquipmentStatusDto>> GetStatus(Guid id)
        {
            var customerId = GetCustomerId();

            var status = await _context.EquipmentStatuses
                .FirstOrDefaultAsync(s => s.Id == id && s.CustomerId == customerId);

            if (status == null)
            {
                return NotFound();
            }

            var dto = new EquipmentStatusDto
            {
                Id = status.Id,
                Name = status.Name,
                Code = status.Code,
                Description = status.Description,
                Color = status.Color,
                Icon = status.Icon,
                DisplayOrder = status.DisplayOrder,
                IsActive = status.IsActive,
                IsPredefined = status.IsPredefined
            };

            return Ok(dto);
        }

        // POST: api/equipmentstatuses
        [HttpPost]
        public async Task<ActionResult<EquipmentStatusDto>> CreateStatus(CreateEquipmentStatusRequest request)
        {
            var customerId = GetCustomerId();

            var status = new EquipmentStatus
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                Name = request.Name,
                Code = request.Code,
                Description = request.Description,
                Color = request.Color,
                Icon = request.Icon,
                DisplayOrder = request.DisplayOrder,
                IsPredefined = false
            };

            _context.EquipmentStatuses.Add(status);
            await _context.SaveChangesAsync();

            var dto = new EquipmentStatusDto
            {
                Id = status.Id,
                Name = status.Name,
                Code = status.Code,
                Description = status.Description,
                Color = status.Color,
                Icon = status.Icon,
                DisplayOrder = status.DisplayOrder,
                IsActive = status.IsActive,
                IsPredefined = status.IsPredefined
            };

            return CreatedAtAction(nameof(GetStatus), new { id = status.Id }, dto);
        }

        // PUT: api/equipmentstatuses/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateStatus(Guid id, UpdateEquipmentStatusRequest request)
        {
            var customerId = GetCustomerId();

            var status = await _context.EquipmentStatuses
                .FirstOrDefaultAsync(s => s.Id == id && s.CustomerId == customerId);

            if (status == null)
            {
                return NotFound();
            }

            status.Name = request.Name;
            status.Code = request.Code;
            status.Description = request.Description;
            status.Color = request.Color;
            status.Icon = request.Icon;
            status.DisplayOrder = request.DisplayOrder;
            status.IsActive = request.IsActive;
            status.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/equipmentstatuses/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteStatus(Guid id)
        {
            var customerId = GetCustomerId();

            var status = await _context.EquipmentStatuses
                .FirstOrDefaultAsync(s => s.Id == id && s.CustomerId == customerId);

            if (status == null)
            {
                return NotFound();
            }

            // Vérifier si le statut est utilisé par des équipements
            var assetsCount = await _context.Assets
                .Include(a => a.Zone)
                    .ThenInclude(z => z.Site)
                .Where(a => a.Zone.Site.CustomerId == customerId && a.Status == status.Code)
                .CountAsync();

            if (assetsCount > 0)
            {
                return BadRequest(new { message = $"Ce statut est utilisé par {assetsCount} équipement(s) et ne peut pas être supprimé." });
            }

            _context.EquipmentStatuses.Remove(status);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/equipmentstatuses/init-predefined
        /// <summary>
        /// Initialise les statuts d'équipement prédéfinis pour le client
        /// </summary>
        [HttpPost("init-predefined")]
        public async Task<ActionResult> InitPredefinedStatuses()
        {
            try
            {
                var customerId = GetCustomerId();

                // Vérifier si déjà initialisé
                var existingCount = await _context.EquipmentStatuses
                    .CountAsync(s => s.CustomerId == customerId && s.IsPredefined);

                if (existingCount > 0)
                {
                    return BadRequest(new { message = $"Les statuts prédéfinis ont déjà été initialisés ({existingCount} statuts trouvés)" });
                }

                var statuses = new List<EquipmentStatus>
                {
                    new EquipmentStatus { Id = Guid.NewGuid(), CustomerId = customerId, Name = "Opérationnel", Code = "OPERATIONAL", Description = "Équipement en fonctionnement normal", Color = "#10B981", Icon = "✅", DisplayOrder = 1, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentStatus { Id = Guid.NewGuid(), CustomerId = customerId, Name = "En maintenance", Code = "MAINTENANCE", Description = "Équipement en cours de maintenance", Color = "#F59E0B", Icon = "🔧", DisplayOrder = 2, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentStatus { Id = Guid.NewGuid(), CustomerId = customerId, Name = "Arrêté", Code = "STOPPED", Description = "Équipement arrêté", Color = "#EF4444", Icon = "⏸️", DisplayOrder = 3, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentStatus { Id = Guid.NewGuid(), CustomerId = customerId, Name = "En panne", Code = "BREAKDOWN", Description = "Équipement en panne", Color = "#DC2626", Icon = "⚠️", DisplayOrder = 4, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentStatus { Id = Guid.NewGuid(), CustomerId = customerId, Name = "Hors service", Code = "OUT_OF_SERVICE", Description = "Équipement hors service", Color = "#6B7280", Icon = "❌", DisplayOrder = 5, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow }
                };

                _context.EquipmentStatuses.AddRange(statuses);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"{statuses.Count} statuts prédéfinis créés avec succès", count = statuses.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur lors de l'initialisation : {ex.Message}" });
            }
        }
    }

    // DTOs
    public class EquipmentStatusDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public bool IsPredefined { get; set; }
    }

    public class CreateEquipmentStatusRequest
    {
        public string Name { get; set; } = "";
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; } = 0;
    }

    public class UpdateEquipmentStatusRequest
    {
        public string Name { get; set; } = "";
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }
}
