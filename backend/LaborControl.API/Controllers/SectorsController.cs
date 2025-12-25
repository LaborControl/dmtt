using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaborControl.API.Data;
using LaborControl.API.Models;
using System.Security.Claims;

namespace LaborControl.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class SectorsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SectorsController(ApplicationDbContext context)
        {
            _context = context;
        }

        private Guid GetCustomerId()
        {
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;
            if (string.IsNullOrEmpty(customerIdClaim))
            {
                throw new UnauthorizedAccessException("CustomerId not found in token");
            }
            return Guid.Parse(customerIdClaim);
        }

        // GET: api/sectors
        [HttpGet]
        public async Task<ActionResult<IEnumerable<SectorDto>>> GetSectors(
            [FromQuery] bool? isActive = null)
        {
            try
            {
                var customerId = GetCustomerId();

                var query = _context.Sectors
                    .Where(s => s.CustomerId == customerId);

                if (isActive.HasValue)
                {
                    query = query.Where(s => s.IsActive == isActive.Value);
                }

                var sectors = await query
                    .OrderBy(s => s.DisplayOrder)
                    .ThenBy(s => s.Name)
                    .Select(s => new SectorDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Code = s.Code,
                        Description = s.Description,
                        Color = s.Color,
                        Icon = s.Icon,
                        DisplayOrder = s.DisplayOrder,
                        IsPredefined = s.IsPredefined,
                        IsActive = s.IsActive,
                        CreatedAt = s.CreatedAt,
                        IndustriesCount = s.Industries.Count(i => i.IsActive)
                    })
                    .ToListAsync();

                return Ok(sectors);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur lors de la récupération des secteurs: {ex.Message}");
            }
        }

        // GET: api/sectors/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<SectorDto>> GetSector(Guid id)
        {
            try
            {
                var customerId = GetCustomerId();

                var sector = await _context.Sectors
                    .Where(s => s.Id == id && s.CustomerId == customerId)
                    .Select(s => new SectorDto
                    {
                        Id = s.Id,
                        Name = s.Name,
                        Code = s.Code,
                        Description = s.Description,
                        Color = s.Color,
                        Icon = s.Icon,
                        DisplayOrder = s.DisplayOrder,
                        IsPredefined = s.IsPredefined,
                        IsActive = s.IsActive,
                        CreatedAt = s.CreatedAt,
                        IndustriesCount = s.Industries.Count(i => i.IsActive)
                    })
                    .FirstOrDefaultAsync();

                if (sector == null)
                {
                    return NotFound("Secteur non trouvé");
                }

                return Ok(sector);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur lors de la récupération du secteur: {ex.Message}");
            }
        }

        // POST: api/sectors
        [HttpPost]
        public async Task<ActionResult<SectorDto>> CreateSector([FromBody] CreateSectorRequest request)
        {
            try
            {
                var customerId = GetCustomerId();

                // Vérifier si le nom existe déjà
                var exists = await _context.Sectors
                    .AnyAsync(s => s.CustomerId == customerId && s.Name == request.Name && s.IsActive);

                if (exists)
                {
                    return BadRequest("Un secteur avec ce nom existe déjà");
                }

                var sector = new Sector
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    Name = request.Name,
                    Code = request.Code,
                    Description = request.Description,
                    Color = request.Color,
                    Icon = request.Icon,
                    DisplayOrder = request.DisplayOrder,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Sectors.Add(sector);
                await _context.SaveChangesAsync();

                var dto = new SectorDto
                {
                    Id = sector.Id,
                    Name = sector.Name,
                    Code = sector.Code,
                    Description = sector.Description,
                    Color = sector.Color,
                    Icon = sector.Icon,
                    DisplayOrder = sector.DisplayOrder,
                    IsPredefined = sector.IsPredefined,
                    IsActive = sector.IsActive,
                    CreatedAt = sector.CreatedAt,
                    IndustriesCount = 0
                };

                return CreatedAtAction(nameof(GetSector), new { id = sector.Id }, dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur lors de la création du secteur: {ex.Message}");
            }
        }

        // PUT: api/sectors/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<SectorDto>> UpdateSector(Guid id, [FromBody] UpdateSectorRequest request)
        {
            try
            {
                var customerId = GetCustomerId();

                var sector = await _context.Sectors
                    .FirstOrDefaultAsync(s => s.Id == id && s.CustomerId == customerId);

                if (sector == null)
                {
                    return NotFound("Secteur non trouvé");
                }

                // Vérifier si le nouveau nom existe déjà (sauf pour ce secteur)
                var nameExists = await _context.Sectors
                    .AnyAsync(s => s.CustomerId == customerId &&
                                   s.Name == request.Name &&
                                   s.Id != id &&
                                   s.IsActive);

                if (nameExists)
                {
                    return BadRequest("Un secteur avec ce nom existe déjà");
                }

                sector.Name = request.Name;
                sector.Code = request.Code;
                sector.Description = request.Description;
                sector.Color = request.Color;
                sector.Icon = request.Icon;
                sector.DisplayOrder = request.DisplayOrder;
                sector.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var industriesCount = await _context.Industries
                    .CountAsync(i => i.SectorId == sector.Id && i.IsActive);

                var dto = new SectorDto
                {
                    Id = sector.Id,
                    Name = sector.Name,
                    Code = sector.Code,
                    Description = sector.Description,
                    Color = sector.Color,
                    Icon = sector.Icon,
                    DisplayOrder = sector.DisplayOrder,
                    IsPredefined = sector.IsPredefined,
                    IsActive = sector.IsActive,
                    CreatedAt = sector.CreatedAt,
                    IndustriesCount = industriesCount
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur lors de la mise à jour du secteur: {ex.Message}");
            }
        }

        // DELETE: api/sectors/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteSector(Guid id)
        {
            try
            {
                var customerId = GetCustomerId();

                var sector = await _context.Sectors
                    .Include(s => s.Industries)
                    .FirstOrDefaultAsync(s => s.Id == id && s.CustomerId == customerId);

                if (sector == null)
                {
                    return NotFound("Secteur non trouvé");
                }

                // Vérifier si des métiers sont liés
                var hasActiveIndustries = sector.Industries.Any(i => i.IsActive);
                if (hasActiveIndustries)
                {
                    return BadRequest("Impossible de supprimer ce secteur car des métiers y sont associés. Veuillez d'abord supprimer ou désactiver les métiers.");
                }

                // Soft delete
                sector.IsActive = false;
                sector.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Secteur supprimé avec succès" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur lors de la suppression du secteur: {ex.Message}");
            }
        }

        // POST: api/sectors/{id}/toggle
        /// <summary>
        /// Active ou désactive un secteur (toggle IsActive)
        /// </summary>
        [HttpPost("{id}/toggle")]
        public async Task<ActionResult<SectorDto>> ToggleSector(Guid id)
        {
            try
            {
                var customerId = GetCustomerId();

                var sector = await _context.Sectors
                    .FirstOrDefaultAsync(s => s.Id == id && s.CustomerId == customerId);

                if (sector == null)
                {
                    return NotFound("Secteur non trouvé");
                }

                // Toggle IsActive
                sector.IsActive = !sector.IsActive;
                sector.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var industriesCount = await _context.Industries
                    .CountAsync(i => i.SectorId == sector.Id && i.IsActive);

                var dto = new SectorDto
                {
                    Id = sector.Id,
                    Name = sector.Name,
                    Code = sector.Code,
                    Description = sector.Description,
                    Color = sector.Color,
                    Icon = sector.Icon,
                    DisplayOrder = sector.DisplayOrder,
                    IsPredefined = sector.IsPredefined,
                    IsActive = sector.IsActive,
                    CreatedAt = sector.CreatedAt,
                    IndustriesCount = industriesCount
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur lors du toggle du secteur: {ex.Message}");
            }
        }

        // POST: api/sectors/init-predefined
        /// <summary>
        /// Initialise les secteurs d'activité prédéfinis pour le client (inactifs par défaut)
        /// </summary>
        [HttpPost("init-predefined")]
        public async Task<ActionResult> InitPredefinedSectors()
        {
            try
            {
                var customerId = GetCustomerId();

                // Vérifier si déjà initialisé
                var existingCount = await _context.Sectors
                    .CountAsync(s => s.CustomerId == customerId && s.IsPredefined);

                if (existingCount > 0)
                {
                    return BadRequest($"Les secteurs prédéfinis ont déjà été initialisés ({existingCount} secteurs trouvés)");
                }

                var predefinedSectors = new List<Sector>
                {
                    new Sector { Id = Guid.NewGuid(), CustomerId = customerId, Name = "Maintenance industrielle", Code = "MAINTENANCE", Description = "Maintenance préventive et curative des équipements industriels, électricité, mécanique, automatisme", Color = "#3B82F6", Icon = "🔧", DisplayOrder = 1, IsPredefined = true, IsActive = false, CreatedAt = DateTime.UtcNow },
                    new Sector { Id = Guid.NewGuid(), CustomerId = customerId, Name = "QHSE", Code = "QHSE", Description = "Qualité, Hygiène, Sécurité et Environnement - Prévention des risques professionnels", Color = "#EF4444", Icon = "⚠️", DisplayOrder = 2, IsPredefined = true, IsActive = false, CreatedAt = DateTime.UtcNow },
                    new Sector { Id = Guid.NewGuid(), CustomerId = customerId, Name = "Santé et Médico-social", Code = "SANTE", Description = "Secteur de la santé, aide à la personne, établissements médico-sociaux", Color = "#10B981", Icon = "⚕️", DisplayOrder = 3, IsPredefined = true, IsActive = false, CreatedAt = DateTime.UtcNow },
                    new Sector { Id = Guid.NewGuid(), CustomerId = customerId, Name = "Nettoyage et Propreté", Code = "NETTOYAGE", Description = "Services de nettoyage industriel, tertiaire, entretien des locaux", Color = "#8B5CF6", Icon = "🧹", DisplayOrder = 4, IsPredefined = true, IsActive = false, CreatedAt = DateTime.UtcNow },
                    new Sector { Id = Guid.NewGuid(), CustomerId = customerId, Name = "Sécurité et Gardiennage", Code = "SECURITE", Description = "Agent de sécurité, gardiennage, surveillance, sûreté", Color = "#F59E0B", Icon = "🛡️", DisplayOrder = 5, IsPredefined = true, IsActive = false, CreatedAt = DateTime.UtcNow },
                    new Sector { Id = Guid.NewGuid(), CustomerId = customerId, Name = "Commerce et Vente", Code = "COMMERCE", Description = "Grande distribution, commerce de détail, vente", Color = "#EC4899", Icon = "🛒", DisplayOrder = 6, IsPredefined = true, IsActive = false, CreatedAt = DateTime.UtcNow },
                    new Sector { Id = Guid.NewGuid(), CustomerId = customerId, Name = "Restauration et Hôtellerie", Code = "RESTAURATION", Description = "Restauration collective, restauration rapide, hôtellerie", Color = "#F97316", Icon = "🍽️", DisplayOrder = 7, IsPredefined = true, IsActive = false, CreatedAt = DateTime.UtcNow },
                    new Sector { Id = Guid.NewGuid(), CustomerId = customerId, Name = "Logistique et Transport", Code = "LOGISTIQUE", Description = "Entrepôt, préparation de commandes, manutention, livraison", Color = "#06B6D4", Icon = "📦", DisplayOrder = 8, IsPredefined = true, IsActive = false, CreatedAt = DateTime.UtcNow },
                    new Sector { Id = Guid.NewGuid(), CustomerId = customerId, Name = "BTP", Code = "BTP", Description = "Bâtiment, travaux publics, génie civil, construction", Color = "#6366F1", Icon = "🏗️", DisplayOrder = 9, IsPredefined = true, IsActive = false, CreatedAt = DateTime.UtcNow },
                    new Sector { Id = Guid.NewGuid(), CustomerId = customerId, Name = "Informatique et Digital", Code = "IT", Description = "Support informatique, développement, infrastructure IT", Color = "#14B8A6", Icon = "💻", DisplayOrder = 10, IsPredefined = true, IsActive = false, CreatedAt = DateTime.UtcNow }
                };

                _context.Sectors.AddRange(predefinedSectors);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"{predefinedSectors.Count} secteurs prédéfinis initialisés avec succès (tous inactifs par défaut)", count = predefinedSectors.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur lors de l'initialisation des secteurs prédéfinis: {ex.Message}");
            }
        }
    }

    // DTOs
    public class SectorDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsPredefined { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int IndustriesCount { get; set; }
    }

    public class CreateSectorRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; } = 0;
    }

    public class UpdateSectorRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; }
    }
}
