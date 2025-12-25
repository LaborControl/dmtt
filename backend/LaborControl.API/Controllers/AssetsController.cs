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
    public class AssetsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<AssetsController> _logger;

        public AssetsController(ApplicationDbContext context, ILogger<AssetsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private Guid GetCustomerId()
        {
            var customerIdClaim = HttpContext.User.FindFirst("CustomerId")?.Value;
            return Guid.TryParse(customerIdClaim, out var customerId) ? customerId : Guid.Empty;
        }

        // GET: api/assets?zoneId={guid} (optionnel)
        [HttpGet]
        public async Task<ActionResult<IEnumerable<AssetDto>>> GetAssets([FromQuery] Guid? zoneId)
        {
            try
            {
                var customerId = Guid.Parse(User.FindFirst("CustomerId")?.Value ?? throw new Exception("CustomerId non trouvé"));

                var query = _context.Assets
                    .Where(a => a.Zone!.Site!.CustomerId == customerId && a.IsActive);

                // Si zoneId est fourni, filtrer par zone ET toutes ses sous-zones
                if (zoneId.HasValue)
                {
                    // Récupérer toutes les zones pour construire l'arbre hiérarchique
                    var allZones = await _context.Zones
                        .Where(z => z.IsActive && z.Site!.CustomerId == customerId)
                        .ToListAsync();

                    // Fonction récursive pour obtenir tous les IDs de sous-zones
                    List<Guid> GetAllSubZoneIds(Guid parentId)
                    {
                        var subZones = allZones.Where(z => z.ParentZoneId == parentId).ToList();
                        var ids = subZones.Select(z => z.Id).ToList();

                        foreach (var subZone in subZones)
                        {
                            ids.AddRange(GetAllSubZoneIds(subZone.Id));
                        }

                        return ids;
                    }

                    // Obtenir la zone sélectionnée + toutes ses sous-zones
                    var zoneIds = new List<Guid> { zoneId.Value };
                    zoneIds.AddRange(GetAllSubZoneIds(zoneId.Value));

                    query = query.Where(a => zoneIds.Contains(a.ZoneId));
                }

                // Optimisation: AsNoTracking pour lecture seule, pas besoin d'Include avant Select
                var assets = await query
                    .AsNoTracking()
                    .OrderBy(a => a.Level)
                    .ThenBy(a => a.DisplayOrder)
                    .ThenBy(a => a.Name)
                    .Select(a => new AssetDto
                    {
                        Id = a.Id,
                        ZoneId = a.ZoneId,
                        ZoneName = a.Zone == null ? "" : a.Zone.Name,
                        Name = a.Name,
                        Code = a.Code,
                        Type = a.Type,
                        Category = a.Category,
                        Status = a.Status,
                        ParentAssetId = a.ParentAssetId,
                        Level = a.Level,
                        DisplayOrder = a.DisplayOrder,
                        Manufacturer = a.Manufacturer,
                        Model = a.Model,
                        SerialNumber = a.SerialNumber,
                        InstallationDate = a.InstallationDate,
                        IsActive = a.IsActive,
                        SubAssetsCount = a.SubAssets.Count(sa => sa.IsActive),
                        ControlPointsCount = a.ControlPoints.Count(cp => cp.IsActive)
                    })
                    .ToListAsync();

                _logger.LogInformation($"DEBUG: {assets.Count} équipements chargés pour le customer {customerId}" + (zoneId.HasValue ? $" dans la zone {zoneId.Value} et ses sous-zones" : " (toutes zones)"));

                return Ok(assets);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des équipements");
                return StatusCode(500, "Erreur serveur");
            }
        }

        // GET: api/assets/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<AssetDetailDto>> GetAsset(Guid id)
        {
            try
            {
                var asset = await _context.Assets
                    .Include(a => a.Zone)
                    .Include(a => a.ParentAsset)
                    .Include(a => a.SubAssets.Where(sa => sa.IsActive))
                        .ThenInclude(sa => sa.Zone)
                    .Include(a => a.ControlPoints.Where(cp => cp.IsActive))
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (asset == null)
                {
                    return NotFound("Équipement introuvable");
                }

                var assetDetail = new AssetDetailDto
                {
                    Id = asset.Id,
                    ZoneId = asset.ZoneId,
                    ZoneName = asset.Zone?.Name ?? "",
                    Name = asset.Name,
                    Code = asset.Code,
                    Type = asset.Type,
                    Category = asset.Category,
                    Status = asset.Status,
                    ParentAssetId = asset.ParentAssetId,
                    ParentAssetName = asset.ParentAsset?.Name,
                    Level = asset.Level,
                    DisplayOrder = asset.DisplayOrder,
                    TechnicalData = asset.TechnicalData,
                    Manufacturer = asset.Manufacturer,
                    Model = asset.Model,
                    SerialNumber = asset.SerialNumber,
                    InstallationDate = asset.InstallationDate,
                    IsActive = asset.IsActive,
                    SubAssets = asset.SubAssets.Select(sa => new AssetDto
                    {
                        Id = sa.Id,
                        ZoneId = sa.ZoneId,
                        ZoneName = sa.Zone == null ? "" : sa.Zone.Name,
                        Name = sa.Name,
                        Code = sa.Code,
                        Type = sa.Type,
                        Category = sa.Category,
                        Status = sa.Status,
                        Level = sa.Level,
                        DisplayOrder = sa.DisplayOrder,
                        IsActive = sa.IsActive
                    }).ToList(),
                    ControlPoints = asset.ControlPoints.Select(cp => new ControlPointDto
                    {
                        Id = cp.Id,
                        Code = cp.Code,
                        Name = cp.Name,
                        RfidChipId = cp.RfidChipId,
                        HasNfcChip = cp.RfidChipId.HasValue
                    }).ToList()
                };

                return Ok(assetDetail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'équipement {AssetId}", id);
                return StatusCode(500, "Erreur serveur");
            }
        }

        // POST: api/assets
        [HttpPost]
        public async Task<ActionResult<AssetDto>> CreateAsset([FromBody] CreateAssetRequest request)
        {
            try
            {
                // Vérifier que la zone existe
                var zoneExists = await _context.Zones.AnyAsync(z => z.Id == request.ZoneId && z.IsActive);
                if (!zoneExists)
                {
                    return BadRequest("Zone introuvable");
                }

                // Si parent asset spécifié, vérifier qu'il existe
                int level = 0;
                if (request.ParentAssetId.HasValue)
                {
                    var parentAsset = await _context.Assets.FindAsync(request.ParentAssetId.Value);
                    if (parentAsset == null)
                    {
                        return BadRequest("Équipement parent introuvable");
                    }
                    level = parentAsset.Level + 1;
                }

                var asset = new Asset
                {
                    Id = Guid.NewGuid(),
                    ZoneId = request.ZoneId,
                    Name = request.Name,
                    Code = request.Code,
                    Type = request.Type,
                    Category = request.Category,
                    Status = request.Status ?? "OPERATIONAL",
                    ParentAssetId = request.ParentAssetId,
                    Level = level,
                    DisplayOrder = request.DisplayOrder,
                    TechnicalData = request.TechnicalData,
                    Manufacturer = request.Manufacturer,
                    Model = request.Model,
                    SerialNumber = request.SerialNumber,
                    InstallationDate = request.InstallationDate,
                    IsAICyrilleEnabled = request.IsAICyrilleEnabled,
                    IsAIAimeeEnabled = request.IsAIAimeeEnabled,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Assets.Add(asset);
                await _context.SaveChangesAsync();

                var zone = await _context.Zones.FindAsync(request.ZoneId);

                var assetDto = new AssetDto
                {
                    Id = asset.Id,
                    ZoneId = asset.ZoneId,
                    ZoneName = zone?.Name ?? "",
                    Name = asset.Name,
                    Code = asset.Code,
                    Type = asset.Type,
                    Category = asset.Category,
                    Status = asset.Status,
                    ParentAssetId = asset.ParentAssetId,
                    Level = asset.Level,
                    DisplayOrder = asset.DisplayOrder,
                    Manufacturer = asset.Manufacturer,
                    Model = asset.Model,
                    SerialNumber = asset.SerialNumber,
                    InstallationDate = asset.InstallationDate,
                    IsActive = asset.IsActive
                };

                return CreatedAtAction(nameof(GetAsset), new { id = asset.Id }, assetDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de l'équipement");
                return StatusCode(500, "Erreur serveur");
            }
        }

        // PUT: api/assets/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAsset(Guid id, [FromBody] UpdateAssetRequest request)
        {
            try
            {
                var asset = await _context.Assets.FindAsync(id);
                if (asset == null)
                {
                    return NotFound("Équipement introuvable");
                }

                // Vérifier que la zone existe
                var zoneExists = await _context.Zones.AnyAsync(z => z.Id == request.ZoneId && z.IsActive);
                if (!zoneExists)
                {
                    return BadRequest("Zone introuvable");
                }

                // Si changement de parent, recalculer le niveau
                if (request.ParentAssetId != asset.ParentAssetId)
                {
                    if (request.ParentAssetId.HasValue)
                    {
                        // Vérifier qu'on ne crée pas de référence circulaire
                        if (request.ParentAssetId.Value == id)
                        {
                            return BadRequest("Un équipement ne peut pas être son propre parent");
                        }

                        var parentAsset = await _context.Assets.FindAsync(request.ParentAssetId.Value);
                        if (parentAsset == null)
                        {
                            return BadRequest("Équipement parent introuvable");
                        }
                        asset.Level = parentAsset.Level + 1;
                    }
                    else
                    {
                        asset.Level = 0;
                    }
                }

                asset.ZoneId = request.ZoneId;
                asset.Name = request.Name;
                asset.Code = request.Code;
                asset.Type = request.Type;
                asset.Category = request.Category;
                asset.Status = request.Status;
                asset.ParentAssetId = request.ParentAssetId;
                asset.DisplayOrder = request.DisplayOrder;
                asset.TechnicalData = request.TechnicalData;
                asset.Manufacturer = request.Manufacturer;
                asset.Model = request.Model;
                asset.SerialNumber = request.SerialNumber;
                asset.InstallationDate = request.InstallationDate;
                asset.IsActive = request.IsActive;
                asset.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de l'équipement {AssetId}", id);
                return StatusCode(500, "Erreur serveur");
            }
        }

        // DELETE: api/assets/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsset(Guid id)
        {
            try
            {
                var asset = await _context.Assets
                    .Include(a => a.SubAssets)
                    .Include(a => a.ControlPoints)
                    .FirstOrDefaultAsync(a => a.Id == id);

                if (asset == null)
                {
                    return NotFound("Équipement introuvable");
                }

                // Vérifier qu'il n'y a pas de composants actifs
                if (asset.SubAssets.Any(sa => sa.IsActive))
                {
                    return BadRequest("Impossible de supprimer un équipement contenant des composants actifs");
                }

                // Vérifier qu'il n'y a pas de points de contrôle actifs
                if (asset.ControlPoints.Any(cp => cp.IsActive))
                {
                    return BadRequest("Impossible de supprimer un équipement contenant des points de contrôle actifs");
                }

                // Soft delete
                asset.IsActive = false;
                asset.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de l'équipement {AssetId}", id);
                return StatusCode(500, "Erreur serveur");
            }
        }

        // PATCH: api/assets/{id}/operating-hours
        [HttpPatch("{id}/operating-hours")]
        public async Task<IActionResult> UpdateOperatingHours(Guid id, [FromBody] UpdateOperatingHoursRequest request)
        {
            try
            {
                var customerId = GetCustomerId();

                var asset = await _context.Assets
                    .Include(a => a.Zone)
                    .ThenInclude(z => z!.Site)
                    .Where(a => a.Id == id && a.Zone != null && a.Zone.Site != null && a.Zone.Site.CustomerId == customerId)
                    .FirstOrDefaultAsync();

                if (asset == null)
                {
                    return NotFound("Équipement introuvable");
                }

                // Mettre à jour les heures de fonctionnement
                asset.OperatingHours = request.OperatingHours;
                asset.LastOperatingHoursUpdate = DateTime.SpecifyKind(request.LastOperatingHoursUpdate ?? DateTime.UtcNow, DateTimeKind.Utc);
                asset.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new {
                    Message = "Compteur d'heures mis à jour avec succès",
                    OperatingHours = asset.OperatingHours,
                    LastUpdate = asset.LastOperatingHoursUpdate
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du compteur d'heures pour l'équipement {AssetId}", id);
                return StatusCode(500, "Erreur serveur");
            }
        }
    }

    // DTO pour la mise à jour des heures de fonctionnement
    public class UpdateOperatingHoursRequest
    {
        public int OperatingHours { get; set; }
        public DateTime? LastOperatingHoursUpdate { get; set; }
    }
}
