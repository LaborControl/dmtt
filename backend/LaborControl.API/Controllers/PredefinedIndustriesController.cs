using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaborControl.API.Data;
using LaborControl.API.Models;

namespace LaborControl.API.Controllers
{
    /// <summary>
    /// Contrôleur pour gérer les métiers prédéfinis (table de référence maître)
    /// Accessible uniquement par l'équipe Labor Control (app-staff)
    /// </summary>
    [Authorize] // TODO: Ajouter rôle Staff quand le système d'auth staff sera en place
    [ApiController]
    [Route("api/staff/predefined-industries")]
    public class PredefinedIndustriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PredefinedIndustriesController> _logger;

        public PredefinedIndustriesController(ApplicationDbContext context, ILogger<PredefinedIndustriesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/staff/predefined-industries
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PredefinedIndustryDto>>> GetPredefinedIndustries(
            [FromQuery] Guid? predefinedSectorId = null,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                var query = _context.PredefinedIndustries
                    .Include(i => i.PredefinedSector)
                    .AsQueryable();

                if (predefinedSectorId.HasValue)
                {
                    query = query.Where(i => i.PredefinedSectorId == predefinedSectorId.Value);
                }

                if (isActive.HasValue)
                {
                    query = query.Where(i => i.IsActive == isActive.Value);
                }

                var industries = await query
                    .OrderBy(i => i.PredefinedSector!.DisplayOrder)
                    .ThenBy(i => i.DisplayOrder)
                    .ThenBy(i => i.Name)
                    .Select(i => new PredefinedIndustryDto
                    {
                        Id = i.Id,
                        PredefinedSectorId = i.PredefinedSectorId,
                        PredefinedSectorName = i.PredefinedSector != null ? i.PredefinedSector.Name : "",
                        PredefinedSectorColor = i.PredefinedSector != null ? i.PredefinedSector.Color : null,
                        PredefinedSectorIcon = i.PredefinedSector != null ? i.PredefinedSector.Icon : null,
                        Name = i.Name,
                        Code = i.Code,
                        Description = i.Description,
                        Color = i.Color,
                        Icon = i.Icon,
                        DisplayOrder = i.DisplayOrder,
                        RecommendedQualifications = i.RecommendedQualifications,
                        IsActive = i.IsActive,
                        CreatedAt = i.CreatedAt,
                        UpdatedAt = i.UpdatedAt
                    })
                    .ToListAsync();

                return Ok(industries);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des métiers prédéfinis");
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }

        // GET: api/staff/predefined-industries/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<PredefinedIndustryDto>> GetPredefinedIndustry(Guid id)
        {
            try
            {
                var industry = await _context.PredefinedIndustries
                    .Include(i => i.PredefinedSector)
                    .Where(i => i.Id == id)
                    .Select(i => new PredefinedIndustryDto
                    {
                        Id = i.Id,
                        PredefinedSectorId = i.PredefinedSectorId,
                        PredefinedSectorName = i.PredefinedSector != null ? i.PredefinedSector.Name : "",
                        PredefinedSectorColor = i.PredefinedSector != null ? i.PredefinedSector.Color : null,
                        PredefinedSectorIcon = i.PredefinedSector != null ? i.PredefinedSector.Icon : null,
                        Name = i.Name,
                        Code = i.Code,
                        Description = i.Description,
                        Color = i.Color,
                        Icon = i.Icon,
                        DisplayOrder = i.DisplayOrder,
                        RecommendedQualifications = i.RecommendedQualifications,
                        IsActive = i.IsActive,
                        CreatedAt = i.CreatedAt,
                        UpdatedAt = i.UpdatedAt
                    })
                    .FirstOrDefaultAsync();

                if (industry == null)
                {
                    return NotFound(new { message = "Métier prédéfini introuvable" });
                }

                return Ok(industry);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du métier prédéfini {Id}", id);
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }

        // POST: api/staff/predefined-industries
        [HttpPost]
        public async Task<ActionResult<PredefinedIndustryDto>> CreatePredefinedIndustry([FromBody] CreatePredefinedIndustryRequest request)
        {
            try
            {
                // Vérifier que le secteur prédéfini existe
                var sectorExists = await _context.PredefinedSectors
                    .AnyAsync(s => s.Id == request.PredefinedSectorId);

                if (!sectorExists)
                {
                    return BadRequest(new { message = "Secteur prédéfini introuvable" });
                }

                // Vérifier si le nom existe déjà pour ce secteur
                var exists = await _context.PredefinedIndustries
                    .AnyAsync(i => i.PredefinedSectorId == request.PredefinedSectorId &&
                                   i.Name == request.Name);

                if (exists)
                {
                    return BadRequest(new { message = "Un métier prédéfini avec ce nom existe déjà pour ce secteur" });
                }

                var industry = new PredefinedIndustry
                {
                    Id = Guid.NewGuid(),
                    PredefinedSectorId = request.PredefinedSectorId,
                    Name = request.Name,
                    Code = request.Code,
                    Description = request.Description,
                    Color = request.Color,
                    Icon = request.Icon,
                    DisplayOrder = request.DisplayOrder,
                    RecommendedQualifications = request.RecommendedQualifications,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.PredefinedIndustries.Add(industry);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Métier prédéfini créé: {Name} ({Code})", industry.Name, industry.Code);

                // Charger le secteur pour le DTO
                var sector = await _context.PredefinedSectors.FindAsync(request.PredefinedSectorId);

                var dto = new PredefinedIndustryDto
                {
                    Id = industry.Id,
                    PredefinedSectorId = industry.PredefinedSectorId,
                    PredefinedSectorName = sector?.Name ?? "",
                    PredefinedSectorColor = sector?.Color,
                    PredefinedSectorIcon = sector?.Icon,
                    Name = industry.Name,
                    Code = industry.Code,
                    Description = industry.Description,
                    Color = industry.Color,
                    Icon = industry.Icon,
                    DisplayOrder = industry.DisplayOrder,
                    RecommendedQualifications = industry.RecommendedQualifications,
                    IsActive = industry.IsActive,
                    CreatedAt = industry.CreatedAt
                };

                return CreatedAtAction(nameof(GetPredefinedIndustry), new { id = industry.Id }, dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du métier prédéfini");
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }

        // PUT: api/staff/predefined-industries/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdatePredefinedIndustry(Guid id, [FromBody] UpdatePredefinedIndustryRequest request)
        {
            try
            {
                var industry = await _context.PredefinedIndustries.FindAsync(id);

                if (industry == null)
                {
                    return NotFound(new { message = "Métier prédéfini introuvable" });
                }

                // Vérifier que le secteur prédéfini existe
                var sectorExists = await _context.PredefinedSectors
                    .AnyAsync(s => s.Id == request.PredefinedSectorId);

                if (!sectorExists)
                {
                    return BadRequest(new { message = "Secteur prédéfini introuvable" });
                }

                // Vérifier si le nouveau nom existe déjà pour ce secteur (sauf pour ce métier)
                var nameExists = await _context.PredefinedIndustries
                    .AnyAsync(i => i.PredefinedSectorId == request.PredefinedSectorId &&
                                   i.Name == request.Name &&
                                   i.Id != id);

                if (nameExists)
                {
                    return BadRequest(new { message = "Un métier prédéfini avec ce nom existe déjà pour ce secteur" });
                }

                industry.PredefinedSectorId = request.PredefinedSectorId;
                industry.Name = request.Name;
                industry.Code = request.Code;
                industry.Description = request.Description;
                industry.Color = request.Color;
                industry.Icon = request.Icon;
                industry.DisplayOrder = request.DisplayOrder;
                industry.RecommendedQualifications = request.RecommendedQualifications;
                industry.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Métier prédéfini mis à jour: {Name} ({Id})", industry.Name, id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du métier prédéfini {Id}", id);
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }

        // POST: api/staff/predefined-industries/{id}/toggle
        [HttpPost("{id}/toggle")]
        public async Task<ActionResult> TogglePredefinedIndustry(Guid id)
        {
            try
            {
                var industry = await _context.PredefinedIndustries.FindAsync(id);

                if (industry == null)
                {
                    return NotFound(new { message = "Métier prédéfini introuvable" });
                }

                industry.IsActive = !industry.IsActive;
                industry.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Métier prédéfini {Name} ({Id}) {Status}",
                    industry.Name, id, industry.IsActive ? "activé" : "désactivé");

                return Ok(new { isActive = industry.IsActive });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du toggle du métier prédéfini {Id}", id);
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }

        // DELETE: api/staff/predefined-industries/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeletePredefinedIndustry(Guid id)
        {
            try
            {
                var industry = await _context.PredefinedIndustries.FindAsync(id);

                if (industry == null)
                {
                    return NotFound(new { message = "Métier prédéfini introuvable" });
                }

                _context.PredefinedIndustries.Remove(industry);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Métier prédéfini supprimé: {Name} ({Id})", industry.Name, id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du métier prédéfini {Id}", id);
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }
    }

    // DTOs
    public class PredefinedIndustryDto
    {
        public Guid Id { get; set; }
        public Guid PredefinedSectorId { get; set; }
        public string PredefinedSectorName { get; set; } = string.Empty;
        public string? PredefinedSectorColor { get; set; }
        public string? PredefinedSectorIcon { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; }
        public string? RecommendedQualifications { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class CreatePredefinedIndustryRequest
    {
        public Guid PredefinedSectorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; } = 0;
        public string? RecommendedQualifications { get; set; }
    }

    public class UpdatePredefinedIndustryRequest
    {
        public Guid PredefinedSectorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; }
        public string? RecommendedQualifications { get; set; }
    }
}
