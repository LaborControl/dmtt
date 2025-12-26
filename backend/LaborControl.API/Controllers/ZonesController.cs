using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaborControl.API.Data;
using LaborControl.API.Models;
using LaborControl.API.DTOs;

namespace LaborControl.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ZonesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ZonesController> _logger;

        public ZonesController(ApplicationDbContext context, ILogger<ZonesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/zones?siteId={guid}
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ZoneDto>>> GetZones([FromQuery] Guid? siteId)
        {
            try
            {
                // Récupérer le CustomerId depuis le token
                var customerIdClaim = User.FindFirst("CustomerId");
                if (customerIdClaim == null || !Guid.TryParse(customerIdClaim.Value, out var customerId))
                {
                    return BadRequest(new { message = "CustomerId introuvable dans le token" });
                }

                // Optimisation: Charger les zones avec AsNoTracking
                var allZones = await _context.Zones
                    .AsNoTracking()
                    .Include(z => z.Site)
                    .Where(z => z.Site != null && z.Site.CustomerId == customerId && z.IsActive)
                    .ToListAsync();

                // Optimisation: Calculer les counts en une seule requête pour éviter N+1
                var zoneIds = allZones.Select(z => z.Id).ToList();
                var assetsCounts = await _context.Assets
                    .Where(a => zoneIds.Contains(a.ZoneId) && a.IsActive)
                    .GroupBy(a => a.ZoneId)
                    .Select(g => new { ZoneId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.ZoneId, x => x.Count);

                var controlPointsCounts = await _context.ControlPoints
                    .Where(cp => cp.ZoneId.HasValue && zoneIds.Contains(cp.ZoneId.Value) && cp.IsActive)
                    .GroupBy(cp => cp.ZoneId!.Value)
                    .Select(g => new { ZoneId = g.Key, Count = g.Count() })
                    .ToDictionaryAsync(x => x.ZoneId, x => x.Count);

                // Si siteId spécifié, filtrer par site
                var zonesToReturn = siteId.HasValue
                    ? allZones.Where(z => z.SiteId == siteId.Value).ToList()
                    : allZones;

                // Créer un dictionnaire pour recherche rapide
                var zoneDictionary = allZones.ToDictionary(z => z.Id, z => z);

                // Fonction pour construire le chemin complet
                string BuildFullPath(Zone zone)
                {
                    var path = new List<string>();
                    var current = zone;

                    while (current != null)
                    {
                        path.Insert(0, current.Name);
                        current = current.ParentZoneId.HasValue && zoneDictionary.ContainsKey(current.ParentZoneId.Value)
                            ? zoneDictionary[current.ParentZoneId.Value]
                            : null;
                    }

                    return string.Join(" / ", path);
                }

                var zones = zonesToReturn
                    .OrderBy(z => z.Site == null ? "" : z.Site.Name)
                    .ThenBy(z => z.Level)
                    .ThenBy(z => z.DisplayOrder)
                    .ThenBy(z => z.Name)
                    .Select(z => new ZoneDto
                    {
                        Id = z.Id,
                        SiteId = z.SiteId,
                        SiteName = z.Site == null ? "" : z.Site.Name,
                        Name = z.Name,
                        FullPath = BuildFullPath(z),
                        Code = z.Code,
                        Type = z.Type,
                        Description = z.Description,
                        ParentZoneId = z.ParentZoneId,
                        Level = z.Level,
                        DisplayOrder = z.DisplayOrder,
                        IsActive = z.IsActive,
                        AssetsCount = assetsCounts.GetValueOrDefault(z.Id, 0),
                        ControlPointsCount = controlPointsCounts.GetValueOrDefault(z.Id, 0)
                    })
                    .ToList();

                return Ok(zones);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des zones");
                return StatusCode(500, "Erreur serveur");
            }
        }

        // GET: api/zones/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ZoneDetailDto>> GetZone(Guid id)
        {
            try
            {
                // Récupérer le CustomerId depuis le token
                var customerIdClaim = User.FindFirst("CustomerId");
                if (customerIdClaim == null || !Guid.TryParse(customerIdClaim.Value, out var customerId))
                {
                    return BadRequest(new { message = "CustomerId introuvable dans le token" });
                }

                var zone = await _context.Zones
                    .Include(z => z.Site)
                    .Include(z => z.ParentZone)
                    .Include(z => z.SubZones.Where(sz => sz.IsActive))
                    .Include(z => z.Assets.Where(a => a.IsActive))
                    .Include(z => z.ControlPoints.Where(cp => cp.IsActive))
                    .FirstOrDefaultAsync(z => z.Id == id);

                if (zone == null)
                {
                    return NotFound("Zone introuvable");
                }

                // Vérifier que la zone appartient au bon client
                if (zone.Site == null || zone.Site.CustomerId != customerId)
                {
                    return NotFound("Zone introuvable");
                }

                var zoneDetail = new ZoneDetailDto
                {
                    Id = zone.Id,
                    SiteId = zone.SiteId,
                    SiteName = zone.Site == null ? "" : zone.Site.Name,
                    Name = zone.Name,
                    Code = zone.Code,
                    Type = zone.Type,
                    Description = zone.Description,
                    ParentZoneId = zone.ParentZoneId,
                    ParentZoneName = zone.ParentZone == null ? null : zone.ParentZone.Name,
                    Level = zone.Level,
                    DisplayOrder = zone.DisplayOrder,
                    IsActive = zone.IsActive,
                    SubZones = zone.SubZones.Select(sz => new ZoneDto
                    {
                        Id = sz.Id,
                        SiteId = sz.SiteId,
                        Name = sz.Name,
                        Code = sz.Code,
                        Type = sz.Type,
                        Level = sz.Level,
                        IsActive = sz.IsActive
                    }).ToList(),
                    Assets = zone.Assets.Select(a => new AssetDto
                    {
                        Id = a.Id,
                        Name = a.Name,
                        Code = a.Code,
                        Type = a.Type,
                        Status = a.Status
                    }).ToList(),
                    ControlPoints = zone.ControlPoints.Select(cp => new ControlPointDto
                    {
                        Id = cp.Id,
                        Code = cp.Code,
                        Name = cp.Name,
                        RfidChipId = cp.RfidChipId,
                        HasNfcChip = cp.RfidChipId.HasValue
                    }).ToList()
                };

                return Ok(zoneDetail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la zone {ZoneId}", id);
                return StatusCode(500, "Erreur serveur");
            }
        }

        // POST: api/zones
        [HttpPost]
        public async Task<ActionResult<ZoneDto>> CreateZone([FromBody] CreateZoneRequest request)
        {
            try
            {
                // Vérifier que le site existe
                var siteExists = await _context.Sites.AnyAsync(s => s.Id == request.SiteId && s.IsActive);
                if (!siteExists)
                {
                    return BadRequest("Site introuvable");
                }

                // Si parent zone spécifiée, vérifier qu'elle existe
                int level = 0;
                if (request.ParentZoneId.HasValue)
                {
                    var parentZone = await _context.Zones.FindAsync(request.ParentZoneId.Value);
                    if (parentZone == null)
                    {
                        return BadRequest("Zone parente introuvable");
                    }
                    level = parentZone.Level + 1;
                }

                var zone = new Zone
                {
                    Id = Guid.NewGuid(),
                    SiteId = request.SiteId,
                    Name = request.Name,
                    Code = request.Code,
                    Type = request.Type,
                    Description = request.Description,
                    ParentZoneId = request.ParentZoneId,
                    Level = level,
                    DisplayOrder = request.DisplayOrder,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Zones.Add(zone);
                await _context.SaveChangesAsync();

                var zoneDto = new ZoneDto
                {
                    Id = zone.Id,
                    SiteId = zone.SiteId,
                    Name = zone.Name,
                    Code = zone.Code,
                    Type = zone.Type,
                    Description = zone.Description,
                    ParentZoneId = zone.ParentZoneId,
                    Level = zone.Level,
                    DisplayOrder = zone.DisplayOrder,
                    IsActive = zone.IsActive
                };

                return CreatedAtAction(nameof(GetZone), new { id = zone.Id }, zoneDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la zone");
                return StatusCode(500, "Erreur serveur");
            }
        }

        // PUT: api/zones/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateZone(Guid id, [FromBody] UpdateZoneRequest request)
        {
            try
            {
                var zone = await _context.Zones.FindAsync(id);
                if (zone == null)
                {
                    return NotFound("Zone introuvable");
                }

                // Si changement de parent, recalculer le niveau
                if (request.ParentZoneId != zone.ParentZoneId)
                {
                    if (request.ParentZoneId.HasValue)
                    {
                        var parentZone = await _context.Zones.FindAsync(request.ParentZoneId.Value);
                        if (parentZone == null)
                        {
                            return BadRequest("Zone parente introuvable");
                        }
                        zone.Level = parentZone.Level + 1;
                    }
                    else
                    {
                        zone.Level = 0;
                    }
                }

                zone.Name = request.Name;
                zone.Code = request.Code;
                zone.Type = request.Type;
                zone.Description = request.Description;
                zone.ParentZoneId = request.ParentZoneId;
                zone.DisplayOrder = request.DisplayOrder;
                zone.IsActive = request.IsActive;
                zone.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la zone {ZoneId}", id);
                return StatusCode(500, "Erreur serveur");
            }
        }

        // DELETE: api/zones/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteZone(Guid id)
        {
            try
            {
                var zone = await _context.Zones
                    .Include(z => z.SubZones)
                    .Include(z => z.Assets)
                    .Include(z => z.ControlPoints)
                    .FirstOrDefaultAsync(z => z.Id == id);

                if (zone == null)
                {
                    return NotFound("Zone introuvable");
                }

                // Vérifier qu'il n'y a pas de sous-zones actives
                if (zone.SubZones.Any(sz => sz.IsActive))
                {
                    return BadRequest("Impossible de supprimer une zone contenant des sous-zones actives");
                }

                // Vérifier qu'il n'y a pas d'équipements actifs
                if (zone.Assets.Any(a => a.IsActive))
                {
                    return BadRequest("Impossible de supprimer une zone contenant des équipements actifs");
                }

                // Vérifier qu'il n'y a pas de points de contrôle actifs
                if (zone.ControlPoints.Any(cp => cp.IsActive))
                {
                    return BadRequest("Impossible de supprimer une zone contenant des points de contrôle actifs");
                }

                // Soft delete
                zone.IsActive = false;
                zone.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la zone {ZoneId}", id);
                return StatusCode(500, "Erreur serveur");
            }
        }

        /// <summary>
        /// Récupère tous les équipements d'une zone
        /// </summary>
        [HttpGet("{id}/assets")]
        public async Task<ActionResult<List<AssetDto>>> GetZoneAssets(Guid id)
        {
            try
            {
                // Vérifier que la zone existe
                var zoneExists = await _context.Zones.AnyAsync(z => z.Id == id && z.IsActive);
                if (!zoneExists)
                {
                    return NotFound("Zone introuvable");
                }

                var assets = await _context.Assets
                    .Where(a => a.ZoneId == id && a.IsActive)
                    .OrderBy(a => a.Name)
                    .Select(a => new AssetDto
                    {
                        Id = a.Id,
                        Name = a.Name,
                        Code = a.Code,
                        Type = a.Type,
                        Status = a.Status
                    })
                    .ToListAsync();

                return Ok(assets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des équipements de la zone {ZoneId}", id);
                return StatusCode(500, "Erreur serveur");
            }
        }
    }
}
