using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaborControl.API.Data;
using LaborControl.API.Models;

namespace LaborControl.API.Controllers
{
    /// <summary>
    /// Contrôleur pour gérer les secteurs prédéfinis (table de référence maître)
    /// Accessible uniquement par l'équipe Labor Control (app-staff)
    /// </summary>
    [Authorize] // TODO: Ajouter rôle Staff quand le système d'auth staff sera en place
    [ApiController]
    [Route("api/staff/predefined-sectors")]
    public class PredefinedSectorsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PredefinedSectorsController> _logger;

        public PredefinedSectorsController(ApplicationDbContext context, ILogger<PredefinedSectorsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/staff/predefined-sectors
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PredefinedSectorDto>>> GetPredefinedSectors(
            [FromQuery] bool? isActive = null)
        {
            try
            {
                var query = _context.PredefinedSectors.AsQueryable();

                if (isActive.HasValue)
                {
                    query = query.Where(s => s.IsActive == isActive.Value);
                }

                var sectors = await query
                    .OrderBy(s => s.DisplayOrder)
                    .ThenBy(s => s.Name)
                    .Select(s => new PredefinedSectorDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Code = s.Code,
                        Description = s.Description,
                        Color = s.Color,
                        Icon = s.Icon,
                        DisplayOrder = s.DisplayOrder,
                        IsActive = s.IsActive,
                        CreatedAt = s.CreatedAt,
                        UpdatedAt = s.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(sectors);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des secteurs prédéfinis");
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }

        // GET: api/staff/predefined-sectors/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<PredefinedSectorDto>> GetPredefinedSector(Guid id)
        {
            try
            {
                var sector = await _context.PredefinedSectors
                    .Where(s => s.Id == id)
                    .Select(s => new PredefinedSectorDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Code = s.Code,
                        Description = s.Description,
                        Color = s.Color,
                        Icon = s.Icon,
                        DisplayOrder = s.DisplayOrder,
                        IsActive = s.IsActive,
                        CreatedAt = s.CreatedAt,
                        UpdatedAt = s.UpdatedAt
                    })
                    .FirstOrDefaultAsync();

                if (sector == null)
                {
                    return NotFound(new { message = "Secteur prédéfini introuvable" });
                }

                return Ok(sector);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du secteur prédéfini {Id}", id);
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }

        // POST: api/staff/predefined-sectors
        [HttpPost]
        public async Task<ActionResult<PredefinedSectorDto>> CreatePredefinedSector([FromBody] CreatePredefinedSectorRequest request)
        {
            try
            {
                // Vérifier si le nom existe déjà
                var exists = await _context.PredefinedSectors
                    .AnyAsync(s => s.Name == request.Name);

                if (exists)
                {
                    return BadRequest(new { message = "Un secteur prédéfini avec ce nom existe déjà" });
                }

                var sector = new PredefinedSector
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    Code = request.Code,
                    Description = request.Description,
                    Color = request.Color,
                    Icon = request.Icon,
                    DisplayOrder = request.DisplayOrder,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.PredefinedSectors.Add(sector);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Secteur prédéfini créé: {Name} ({Code})", sector.Name, sector.Code);

                var dto = new PredefinedSectorDto
                {
                    Id = sector.Id,
                    Name = sector.Name,
                    Code = sector.Code,
                    Description = sector.Description,
                    Color = sector.Color,
                    Icon = sector.Icon,
                    DisplayOrder = sector.DisplayOrder,
                    IsActive = sector.IsActive,
                    CreatedAt = sector.CreatedAt
                };

                return CreatedAtAction(nameof(GetPredefinedSector), new { id = sector.Id }, dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du secteur prédéfini");
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }

        // PUT: api/staff/predefined-sectors/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdatePredefinedSector(Guid id, [FromBody] UpdatePredefinedSectorRequest request)
        {
            try
            {
                var sector = await _context.PredefinedSectors.FindAsync(id);

                if (sector == null)
                {
                    return NotFound(new { message = "Secteur prédéfini introuvable" });
                }

                // Vérifier si le nouveau nom existe déjà (sauf pour ce secteur)
                var nameExists = await _context.PredefinedSectors
                    .AnyAsync(s => s.Name == request.Name && s.Id != id);

                if (nameExists)
                {
                    return BadRequest(new { message = "Un secteur prédéfini avec ce nom existe déjà" });
                }

                sector.Name = request.Name;
                sector.Code = request.Code;
                sector.Description = request.Description;
                sector.Color = request.Color;
                sector.Icon = request.Icon;
                sector.DisplayOrder = request.DisplayOrder;
                sector.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Secteur prédéfini mis à jour: {Name} ({Id})", sector.Name, id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du secteur prédéfini {Id}", id);
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }

        // POST: api/staff/predefined-sectors/{id}/toggle
        [HttpPost("{id}/toggle")]
        public async Task<ActionResult> TogglePredefinedSector(Guid id)
        {
            try
            {
                var sector = await _context.PredefinedSectors.FindAsync(id);

                if (sector == null)
                {
                    return NotFound(new { message = "Secteur prédéfini introuvable" });
                }

                sector.IsActive = !sector.IsActive;
                sector.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Secteur prédéfini {Name} ({Id}) {Status}",
                    sector.Name, id, sector.IsActive ? "activé" : "désactivé");

                return Ok(new { isActive = sector.IsActive });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du toggle du secteur prédéfini {Id}", id);
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }

        // DELETE: api/staff/predefined-sectors/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeletePredefinedSector(Guid id)
        {
            try
            {
                var sector = await _context.PredefinedSectors.FindAsync(id);

                if (sector == null)
                {
                    return NotFound(new { message = "Secteur prédéfini introuvable" });
                }

                // Vérifier si des métiers prédéfinis sont liés
                var hasIndustries = await _context.PredefinedIndustries
                    .AnyAsync(i => i.PredefinedSectorId == id);

                if (hasIndustries)
                {
                    return BadRequest(new { message = "Impossible de supprimer ce secteur car des métiers prédéfinis y sont associés" });
                }

                _context.PredefinedSectors.Remove(sector);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Secteur prédéfini supprimé: {Name} ({Id})", sector.Name, id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du secteur prédéfini {Id}", id);
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }
    }

    // DTOs
    public class PredefinedSectorDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreatePredefinedSectorRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; } = 0;
    }

    public class UpdatePredefinedSectorRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; }
    }
}
