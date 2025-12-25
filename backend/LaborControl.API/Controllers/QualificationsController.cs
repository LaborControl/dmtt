using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaborControl.API.Data;
using LaborControl.API.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace LaborControl.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class QualificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<QualificationsController> _logger;

        public QualificationsController(ApplicationDbContext context, ILogger<QualificationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private Guid GetCustomerId()
        {
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;
            if (string.IsNullOrEmpty(customerIdClaim) || !Guid.TryParse(customerIdClaim, out var customerId))
            {
                throw new UnauthorizedAccessException("CustomerId introuvable ou invalide dans le token");
            }
            return customerId;
        }

        // ========================================
        // GET: api/qualifications
        // Liste toutes les qualifications du client (actives et inactives)
        // ========================================
        [HttpGet]
        public async Task<ActionResult<IEnumerable<QualificationDto>>> GetQualifications(
            [FromQuery] Guid? sectorId = null,
            [FromQuery] string? category = null,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                var customerId = GetCustomerId();

                var query = _context.Qualifications
                    .Include(q => q.Sector)
                    .Include(q => q.Industry)
                    .Where(q => q.CustomerId == customerId);

                // Filtre par secteur
                if (sectorId.HasValue)
                {
                    query = query.Where(q => q.SectorId == sectorId.Value);
                }

                // Filtre par catégorie
                if (!string.IsNullOrEmpty(category))
                {
                    query = query.Where(q => q.Category == category);
                }

                // Filtre par statut actif (optionnel, par défaut on retourne tout)
                if (isActive.HasValue)
                {
                    query = query.Where(q => q.IsActive == isActive.Value);
                }

                var qualifications = await query
                    .OrderBy(q => q.DisplayOrder)
                    .ThenBy(q => q.Name)
                    .Select(q => new QualificationDto
                    {
                        Id = q.Id,
                        SectorId = q.SectorId ?? Guid.Empty,
                        SectorName = q.Sector != null ? q.Sector.Name : "",
                        SectorColor = q.Sector != null ? q.Sector.Color : null,
                        SectorIcon = q.Sector != null ? q.Sector.Icon : null,
                        IndustryId = q.IndustryId,
                        IndustryName = q.Industry != null ? q.Industry.Name : null,
                        Name = q.Name,
                        Category = q.Category,
                        Code = q.Code,
                        Description = q.Description,
                        Color = q.Color,
                        Icon = q.Icon,
                        RequiresRenewal = q.RequiresRenewal,
                        ValidityPeriodMonths = q.ValidityPeriodMonths,
                        CriticalityLevel = q.CriticalityLevel,
                        DisplayOrder = q.DisplayOrder,
                        IsPredefined = q.IsPredefined,
                        IsActive = q.IsActive,
                        CreatedAt = q.CreatedAt
                    })
                    .ToListAsync();

                return Ok(qualifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des qualifications");
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }

        // ========================================
        // GET: api/qualifications/{id}
        // Détails d'une qualification
        // ========================================
        [HttpGet("{id}")]
        public async Task<ActionResult<QualificationDto>> GetQualification(Guid id)
        {
            try
            {
                var customerId = GetCustomerId();

                var qualification = await _context.Qualifications
                    .Include(q => q.Sector)
                    .Where(q => q.Id == id && q.CustomerId == customerId)
                    .Select(q => new QualificationDto
                    {
                        Id = q.Id,
                        SectorId = q.SectorId ?? Guid.Empty,
                        SectorName = q.Sector != null ? q.Sector.Name : "",
                        SectorColor = q.Sector != null ? q.Sector.Color : null,
                        SectorIcon = q.Sector != null ? q.Sector.Icon : null,
                        IndustryId = q.IndustryId,
                        IndustryName = q.Industry != null ? q.Industry.Name : null,
                        Name = q.Name,
                        Category = q.Category,
                        Code = q.Code,
                        Description = q.Description,
                        Color = q.Color,
                        Icon = q.Icon,
                        RequiresRenewal = q.RequiresRenewal,
                        ValidityPeriodMonths = q.ValidityPeriodMonths,
                        CriticalityLevel = q.CriticalityLevel,
                        DisplayOrder = q.DisplayOrder,
                        IsPredefined = q.IsPredefined,
                        IsActive = q.IsActive,
                        CreatedAt = q.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if (qualification == null)
                {
                    return NotFound(new { message = "Qualification introuvable" });
                }

                return Ok(qualification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la qualification {Id}", id);
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }

        // ========================================
        // POST: api/qualifications/{id}/toggle
        // Activer/Désactiver une qualification
        // ========================================
        [HttpPost("{id}/toggle")]
        public async Task<ActionResult> ToggleQualification(Guid id)
        {
            try
            {
                var customerId = GetCustomerId();

                var qualification = await _context.Qualifications
                    .FirstOrDefaultAsync(q => q.Id == id && q.CustomerId == customerId);

                if (qualification == null)
                {
                    return NotFound(new { message = "Qualification introuvable" });
                }

                // Toggle le statut
                qualification.IsActive = !qualification.IsActive;
                qualification.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Qualification {Name} ({Id}) {Status}",
                    qualification.Name, id, qualification.IsActive ? "activée" : "désactivée");

                return Ok(new { isActive = qualification.IsActive });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du toggle de la qualification {Id}", id);
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }

        // ========================================
        // POST: api/qualifications
        // Créer une nouvelle qualification personnalisée
        // ========================================
        [HttpPost]
        public async Task<ActionResult<QualificationDto>> CreateQualification([FromBody] CreateQualificationRequest request)
        {
            try
            {
                var customerId = GetCustomerId();

                // Vérifier que l'industrie existe
                var sector = await _context.Sectors
                    .FirstOrDefaultAsync(i => i.Id == request.SectorId && i.CustomerId == customerId);

                if (sector == null)
                {
                    return BadRequest(new { message = "Secteur introuvable" });
                }

                // Vérifier si une qualification avec le même nom existe déjà
                var exists = await _context.Qualifications
                    .AnyAsync(q => q.CustomerId == customerId &&
                                   q.SectorId == request.SectorId &&
                                   q.Name == request.Name);

                if (exists)
                {
                    return BadRequest(new { message = "Une qualification avec ce nom existe déjà pour ce secteur" });
                }

                var qualification = new Qualification
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    SectorId = request.SectorId,
                    Name = request.Name,
                    Category = request.Category ?? "AUTRE",
                    Code = request.Code,
                    Description = request.Description,
                    Icon = request.Icon,
                    Color = request.Color,
                    RequiresRenewal = request.RequiresRenewal,
                    ValidityPeriodMonths = request.ValidityPeriodMonths,
                    CriticalityLevel = request.CriticalityLevel,
                    DisplayOrder = request.DisplayOrder,
                    IsPredefined = false,
                    IsActive = true, // Qualifications personnalisées actives par défaut
                    CreatedAt = DateTime.UtcNow
                };

                _context.Qualifications.Add(qualification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Qualification personnalisée créée: {Name} ({Category})", qualification.Name, qualification.Category);

                var dto = new QualificationDto
                {
                    Id = qualification.Id,
                    SectorId = qualification.SectorId ?? Guid.Empty,
                    SectorName = sector.Name,
                    SectorColor = sector.Color,
                    SectorIcon = sector.Icon,
                    Name = qualification.Name,
                    Category = qualification.Category,
                    Code = qualification.Code,
                    Description = qualification.Description,
                    Color = qualification.Color,
                    Icon = qualification.Icon,
                    RequiresRenewal = qualification.RequiresRenewal,
                    ValidityPeriodMonths = qualification.ValidityPeriodMonths,
                    CriticalityLevel = qualification.CriticalityLevel,
                    DisplayOrder = qualification.DisplayOrder,
                    IsPredefined = qualification.IsPredefined,
                    IsActive = qualification.IsActive,
                    CreatedAt = qualification.CreatedAt
                };

                return CreatedAtAction(nameof(GetQualification), new { id = qualification.Id }, dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la qualification");
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }

        // ========================================
        // PUT: api/qualifications/{id}
        // Modifier une qualification personnalisée
        // ========================================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateQualification(Guid id, [FromBody] UpdateQualificationRequest request)
        {
            try
            {
                var customerId = GetCustomerId();

                var qualification = await _context.Qualifications
                    .FirstOrDefaultAsync(q => q.Id == id && q.CustomerId == customerId);

                if (qualification == null)
                {
                    return NotFound(new { message = "Qualification introuvable" });
                }

                // Ne peut pas modifier une qualification prédéfinie
                if (qualification.IsPredefined)
                {
                    return BadRequest(new { message = "Impossible de modifier une qualification prédéfinie" });
                }

                // Vérifier que l'industrie existe
                var sector = await _context.Sectors
                    .FirstOrDefaultAsync(i => i.Id == request.SectorId && i.CustomerId == customerId);

                if (sector == null)
                {
                    return BadRequest(new { message = "Secteur introuvable" });
                }

                qualification.SectorId = request.SectorId;
                qualification.Name = request.Name;
                qualification.Category = request.Category ?? "AUTRE";
                qualification.Code = request.Code;
                qualification.Description = request.Description;
                qualification.Icon = request.Icon;
                qualification.Color = request.Color;
                qualification.RequiresRenewal = request.RequiresRenewal;
                qualification.ValidityPeriodMonths = request.ValidityPeriodMonths;
                qualification.CriticalityLevel = request.CriticalityLevel;
                qualification.DisplayOrder = request.DisplayOrder;
                qualification.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Qualification mise à jour: {Name} ({Id})", qualification.Name, id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la qualification {Id}", id);
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }

        // ========================================
        // DELETE: api/qualifications/{id}
        // Supprimer une qualification personnalisée
        // ========================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteQualification(Guid id)
        {
            try
            {
                var customerId = GetCustomerId();

                var qualification = await _context.Qualifications
                    .FirstOrDefaultAsync(q => q.Id == id && q.CustomerId == customerId);

                if (qualification == null)
                {
                    return NotFound(new { message = "Qualification introuvable" });
                }

                // Ne peut pas supprimer une qualification prédéfinie
                if (qualification.IsPredefined)
                {
                    return BadRequest(new { message = "Impossible de supprimer une qualification prédéfinie. Utilisez la désactivation." });
                }

                // Vérifier si la qualification est utilisée
                var isUsed = await _context.UserQualifications
                    .AnyAsync(uq => uq.QualificationId == id && uq.IsActive);

                if (isUsed)
                {
                    return BadRequest(new { message = "Impossible de supprimer cette qualification car elle est attribuée à des utilisateurs" });
                }

                // Soft delete
                qualification.IsActive = false;
                qualification.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Qualification supprimée: {Name} ({Id})", qualification.Name, id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la qualification {Id}", id);
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }

        // ========================================
        // POST: api/qualifications/init-predefined/{sectorId}
        // Initialise les qualifications prédéfinies pour un secteur donné
        // ========================================
        [HttpPost("init-predefined/{sectorId}")]
        public async Task<ActionResult> InitializePredefinedQualifications(Guid sectorId)
        {
            try
            {
                var customerId = GetCustomerId();

                // Vérifier que le secteur existe
                var sector = await _context.Sectors.FindAsync(sectorId);
                if (sector == null)
                {
                    return NotFound(new { message = "Secteur introuvable" });
                }

                // Trouver le secteur prédéfini correspondant par nom
                var predefinedSector = await _context.PredefinedSectors
                    .FirstOrDefaultAsync(ps => ps.Name == sector.Name);

                if (predefinedSector == null)
                {
                    return Ok(new { message = "Aucun secteur prédéfini correspondant trouvé", count = 0 });
                }

                // Récupérer les qualifications prédéfinies pour ce secteur (en utilisant l'ID du secteur prédéfini)
                var predefinedQualifications = await _context.PredefinedQualifications
                    .Include(pq => pq.QualificationSectors)
                    .ThenInclude(qs => qs.PredefinedSector)
                    .Where(pq => pq.IsActive && pq.QualificationSectors.Any(qs => qs.PredefinedSectorId == predefinedSector.Id))
                    .ToListAsync();

                if (!predefinedQualifications.Any())
                {
                    return Ok(new { message = "Aucune qualification prédéfinie trouvée pour ce secteur", count = 0 });
                }

                // Récupérer les qualifications existantes du customer pour éviter les doublons
                var existingCodes = await _context.Qualifications
                    .Where(q => q.CustomerId == customerId && q.Code != null)
                    .Select(q => q.Code)
                    .ToListAsync();

                var existingNames = await _context.Qualifications
                    .Where(q => q.CustomerId == customerId)
                    .Select(q => q.Name)
                    .ToListAsync();

                int createdCount = 0;

                foreach (var pq in predefinedQualifications)
                {
                    // Vérifier si déjà existante (par code ou nom)
                    if ((pq.Code != null && existingCodes.Contains(pq.Code)) || existingNames.Contains(pq.Name))
                    {
                        continue;
                    }

                    // Mapper vers une catégorie
                    var category = MapQualificationTypeToCategory(pq.Type);

                    var qualification = new Qualification
                    {
                        Id = Guid.NewGuid(),
                        CustomerId = customerId,
                        SectorId = sectorId,
                        Name = pq.Name,
                        Code = pq.Code,
                        Description = pq.Description,
                        Category = category,
                        Color = pq.Color,
                        Icon = pq.Icon,
                        DisplayOrder = pq.DisplayOrder,
                        IsPredefined = true,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        // Si c'est une qualification avec date de fin de validité, calculer la durée en mois
                        RequiresRenewal = pq.DateFinValidite.HasValue,
                        ValidityPeriodMonths = pq.DateFinValidite.HasValue ?
                            (int)((pq.DateFinValidite.Value - DateTime.UtcNow).Days / 30) : null
                    };

                    _context.Qualifications.Add(qualification);
                    createdCount++;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Initialisé {Count} qualifications prédéfinies pour le customer {CustomerId} et le secteur {SectorId}",
                    createdCount, customerId, sectorId);

                return Ok(new
                {
                    message = $"{createdCount} qualifications prédéfinies ajoutées",
                    count = createdCount,
                    sectorName = sector.Name
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'initialisation des qualifications prédéfinies");
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }

        private string MapQualificationTypeToCategory(QualificationType type)
        {
            return type switch
            {
                QualificationType.RNCP => "CERTIFICATION_RNCP",
                QualificationType.RS => "CERTIFICATION_RS",
                QualificationType.CQP => "CERTIFICATION_CQP",
                QualificationType.Habilitation => "HABILITATION",
                _ => "AUTRE"
            };
        }
    }

    // ========================================
    // DTOs
    // ========================================

    public class QualificationDto
    {
        public Guid Id { get; set; }
        public Guid SectorId { get; set; }
        public string SectorName { get; set; } = string.Empty;
        public string? SectorColor { get; set; }
        public string? SectorIcon { get; set; }
        public Guid? IndustryId { get; set; }
        public string? IndustryName { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public bool RequiresRenewal { get; set; }
        public int? ValidityPeriodMonths { get; set; }
        public int CriticalityLevel { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsPredefined { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateQualificationRequest
    {
        public Guid SectorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public string? Color { get; set; }
        public bool RequiresRenewal { get; set; } = false;
        public int? ValidityPeriodMonths { get; set; }
        public int CriticalityLevel { get; set; } = 3;
        public int DisplayOrder { get; set; } = 0;
    }

    public class UpdateQualificationRequest
    {
        public Guid SectorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Category { get; set; }
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public string? Color { get; set; }
        public bool RequiresRenewal { get; set; } = false;
        public int? ValidityPeriodMonths { get; set; }
        public int CriticalityLevel { get; set; } = 3;
        public int DisplayOrder { get; set; } = 0;
    }
}
