using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaborControl.API.Data;
using LaborControl.API.Models;

namespace LaborControl.API.Controllers
{
    /// <summary>
    /// Contrôleur pour gérer les qualifications prédéfinies (RNCP, RS, etc.)
    /// Accessible uniquement par l'équipe Labor Control (app-staff)
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/staff/predefined-qualifications")]
    public class PredefinedQualificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<PredefinedQualificationsController> _logger;

        public PredefinedQualificationsController(ApplicationDbContext context, ILogger<PredefinedQualificationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/staff/predefined-qualifications
        [HttpGet]
        public async Task<ActionResult<IEnumerable<PredefinedQualificationDto>>> GetPredefinedQualifications(
            [FromQuery] bool? isActive = null,
            [FromQuery] QualificationType? type = null,
            [FromQuery] Guid? sectorId = null,
            [FromQuery] string? search = null)
        {
            try
            {
                var query = _context.PredefinedQualifications
                    .Include(q => q.QualificationSectors)
                    .ThenInclude(qs => qs.PredefinedSector)
                    .AsQueryable();

                if (isActive.HasValue)
                {
                    query = query.Where(q => q.IsActive == isActive.Value);
                }

                if (type.HasValue)
                {
                    query = query.Where(q => q.Type == type.Value);
                }

                if (sectorId.HasValue)
                {
                    query = query.Where(q => q.QualificationSectors.Any(qs => qs.PredefinedSectorId == sectorId.Value));
                }

                if (!string.IsNullOrWhiteSpace(search))
                {
                    var searchLower = search.ToLower();
                    query = query.Where(q =>
                        q.Name.ToLower().Contains(searchLower) ||
                        (q.Code != null && q.Code.ToLower().Contains(searchLower)) ||
                        (q.RncpCode != null && q.RncpCode.ToLower().Contains(searchLower)) ||
                        (q.RsCode != null && q.RsCode.ToLower().Contains(searchLower)));
                }

                var qualifications = await query
                    .OrderBy(q => q.DisplayOrder)
                    .ThenBy(q => q.Name)
                    .Select(q => new PredefinedQualificationDto
                    {
                        Id = q.Id,
                        Name = q.Name,
                        Code = q.Code,
                        Description = q.Description,
                        Type = q.Type,
                        TypeLabel = GetTypeLabel(q.Type),
                        RncpCode = q.RncpCode,
                        RsCode = q.RsCode,
                        FranceCompetencesUrl = q.FranceCompetencesUrl,
                        Level = q.Level,
                        Certificateur = q.Certificateur,
                        DateEnregistrement = q.DateEnregistrement,
                        DateFinValidite = q.DateFinValidite,
                        Color = q.Color,
                        Icon = q.Icon,
                        DisplayOrder = q.DisplayOrder,
                        IsActive = q.IsActive,
                        CreatedAt = q.CreatedAt,
                        UpdatedAt = q.UpdatedAt,
                        Sectors = q.QualificationSectors.Select(qs => new PredefinedQualificationSectorDto
                        {
                            Id = qs.PredefinedSector.Id,
                            Name = qs.PredefinedSector.Name,
                            Code = qs.PredefinedSector.Code,
                            Color = qs.PredefinedSector.Color,
                            Icon = qs.PredefinedSector.Icon
                        }).ToList()
                    })
                    .ToListAsync();

                return Ok(qualifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des qualifications prédéfinies");
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }

        // GET: api/staff/predefined-qualifications/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<PredefinedQualificationDto>> GetPredefinedQualification(Guid id)
        {
            try
            {
                var qualification = await _context.PredefinedQualifications
                    .Include(q => q.QualificationSectors)
                    .ThenInclude(qs => qs.PredefinedSector)
                    .Where(q => q.Id == id)
                    .Select(q => new PredefinedQualificationDto
                    {
                        Id = q.Id,
                        Name = q.Name,
                        Code = q.Code,
                        Description = q.Description,
                        Type = q.Type,
                        TypeLabel = GetTypeLabel(q.Type),
                        RncpCode = q.RncpCode,
                        RsCode = q.RsCode,
                        FranceCompetencesUrl = q.FranceCompetencesUrl,
                        Level = q.Level,
                        Certificateur = q.Certificateur,
                        DateEnregistrement = q.DateEnregistrement,
                        DateFinValidite = q.DateFinValidite,
                        Color = q.Color,
                        Icon = q.Icon,
                        DisplayOrder = q.DisplayOrder,
                        IsActive = q.IsActive,
                        CreatedAt = q.CreatedAt,
                        UpdatedAt = q.UpdatedAt,
                        Sectors = q.QualificationSectors.Select(qs => new PredefinedQualificationSectorDto
                        {
                            Id = qs.PredefinedSector.Id,
                            Name = qs.PredefinedSector.Name,
                            Code = qs.PredefinedSector.Code,
                            Color = qs.PredefinedSector.Color,
                            Icon = qs.PredefinedSector.Icon
                        }).ToList()
                    })
                    .FirstOrDefaultAsync();

                if (qualification == null)
                {
                    return NotFound(new { message = "Qualification prédéfinie introuvable" });
                }

                return Ok(qualification);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de la qualification prédéfinie {Id}", id);
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }

        // POST: api/staff/predefined-qualifications
        [HttpPost]
        public async Task<ActionResult<PredefinedQualificationDto>> CreatePredefinedQualification([FromBody] CreatePredefinedQualificationRequest request)
        {
            try
            {
                // Vérifier si le nom existe déjà
                var exists = await _context.PredefinedQualifications
                    .AnyAsync(q => q.Name == request.Name);

                if (exists)
                {
                    return BadRequest(new { message = "Une qualification prédéfinie avec ce nom existe déjà" });
                }

                // Vérifier si les codes RNCP/RS sont uniques s'ils sont fournis
                if (!string.IsNullOrEmpty(request.RncpCode))
                {
                    var rncpExists = await _context.PredefinedQualifications
                        .AnyAsync(q => q.RncpCode == request.RncpCode);
                    if (rncpExists)
                    {
                        return BadRequest(new { message = $"Le code RNCP {request.RncpCode} est déjà utilisé" });
                    }
                }

                if (!string.IsNullOrEmpty(request.RsCode))
                {
                    var rsExists = await _context.PredefinedQualifications
                        .AnyAsync(q => q.RsCode == request.RsCode);
                    if (rsExists)
                    {
                        return BadRequest(new { message = $"Le code RS {request.RsCode} est déjà utilisé" });
                    }
                }

                // Vérifier que les secteurs existent
                if (request.SectorIds != null && request.SectorIds.Any())
                {
                    var validSectorCount = await _context.PredefinedSectors
                        .CountAsync(s => request.SectorIds.Contains(s.Id));

                    if (validSectorCount != request.SectorIds.Count)
                    {
                        return BadRequest(new { message = "Un ou plusieurs secteurs sont invalides" });
                    }
                }

                var qualification = new PredefinedQualification
                {
                    Id = Guid.NewGuid(),
                    Name = request.Name,
                    Code = request.Code,
                    Description = request.Description,
                    Type = request.Type,
                    RncpCode = request.RncpCode,
                    RsCode = request.RsCode,
                    FranceCompetencesUrl = request.FranceCompetencesUrl,
                    Level = request.Level,
                    Certificateur = request.Certificateur,
                    DateEnregistrement = request.DateEnregistrement,
                    DateFinValidite = request.DateFinValidite,
                    Color = request.Color,
                    Icon = request.Icon,
                    DisplayOrder = request.DisplayOrder,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.PredefinedQualifications.Add(qualification);

                // Ajouter les associations avec les secteurs
                if (request.SectorIds != null && request.SectorIds.Any())
                {
                    foreach (var sectorId in request.SectorIds)
                    {
                        _context.PredefinedQualificationSectors.Add(new PredefinedQualificationSector
                        {
                            PredefinedQualificationId = qualification.Id,
                            PredefinedSectorId = sectorId
                        });
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Qualification prédéfinie créée: {Name} ({Type})", qualification.Name, qualification.Type);

                // Recharger avec les secteurs
                var createdQualification = await GetPredefinedQualification(qualification.Id);
                return CreatedAtAction(nameof(GetPredefinedQualification), new { id = qualification.Id }, (createdQualification.Result as OkObjectResult)?.Value);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création de la qualification prédéfinie");
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }

        // PUT: api/staff/predefined-qualifications/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult> UpdatePredefinedQualification(Guid id, [FromBody] UpdatePredefinedQualificationRequest request)
        {
            try
            {
                var qualification = await _context.PredefinedQualifications
                    .Include(q => q.QualificationSectors)
                    .FirstOrDefaultAsync(q => q.Id == id);

                if (qualification == null)
                {
                    return NotFound(new { message = "Qualification prédéfinie introuvable" });
                }

                // Vérifier si le nouveau nom existe déjà (sauf pour cette qualification)
                var nameExists = await _context.PredefinedQualifications
                    .AnyAsync(q => q.Name == request.Name && q.Id != id);

                if (nameExists)
                {
                    return BadRequest(new { message = "Une qualification prédéfinie avec ce nom existe déjà" });
                }

                // Vérifier unicité RNCP/RS
                if (!string.IsNullOrEmpty(request.RncpCode))
                {
                    var rncpExists = await _context.PredefinedQualifications
                        .AnyAsync(q => q.RncpCode == request.RncpCode && q.Id != id);
                    if (rncpExists)
                    {
                        return BadRequest(new { message = $"Le code RNCP {request.RncpCode} est déjà utilisé" });
                    }
                }

                if (!string.IsNullOrEmpty(request.RsCode))
                {
                    var rsExists = await _context.PredefinedQualifications
                        .AnyAsync(q => q.RsCode == request.RsCode && q.Id != id);
                    if (rsExists)
                    {
                        return BadRequest(new { message = $"Le code RS {request.RsCode} est déjà utilisé" });
                    }
                }

                // Vérifier que les secteurs existent
                if (request.SectorIds != null && request.SectorIds.Any())
                {
                    var validSectorCount = await _context.PredefinedSectors
                        .CountAsync(s => request.SectorIds.Contains(s.Id));

                    if (validSectorCount != request.SectorIds.Count)
                    {
                        return BadRequest(new { message = "Un ou plusieurs secteurs sont invalides" });
                    }
                }

                // Mettre à jour les champs
                qualification.Name = request.Name;
                qualification.Code = request.Code;
                qualification.Description = request.Description;
                qualification.Type = request.Type;
                qualification.RncpCode = request.RncpCode;
                qualification.RsCode = request.RsCode;
                qualification.FranceCompetencesUrl = request.FranceCompetencesUrl;
                qualification.Level = request.Level;
                qualification.Certificateur = request.Certificateur;
                qualification.DateEnregistrement = request.DateEnregistrement;
                qualification.DateFinValidite = request.DateFinValidite;
                qualification.Color = request.Color;
                qualification.Icon = request.Icon;
                qualification.DisplayOrder = request.DisplayOrder;
                qualification.UpdatedAt = DateTime.UtcNow;

                // Mettre à jour les associations avec les secteurs
                _context.PredefinedQualificationSectors.RemoveRange(qualification.QualificationSectors);

                if (request.SectorIds != null && request.SectorIds.Any())
                {
                    foreach (var sectorId in request.SectorIds)
                    {
                        _context.PredefinedQualificationSectors.Add(new PredefinedQualificationSector
                        {
                            PredefinedQualificationId = qualification.Id,
                            PredefinedSectorId = sectorId
                        });
                    }
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("Qualification prédéfinie mise à jour: {Name} ({Id})", qualification.Name, id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la qualification prédéfinie {Id}", id);
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }

        // POST: api/staff/predefined-qualifications/{id}/toggle
        [HttpPost("{id}/toggle")]
        public async Task<ActionResult> TogglePredefinedQualification(Guid id)
        {
            try
            {
                var qualification = await _context.PredefinedQualifications.FindAsync(id);

                if (qualification == null)
                {
                    return NotFound(new { message = "Qualification prédéfinie introuvable" });
                }

                qualification.IsActive = !qualification.IsActive;
                qualification.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Qualification prédéfinie {Name} ({Id}) {Status}",
                    qualification.Name, id, qualification.IsActive ? "activée" : "désactivée");

                return Ok(new { isActive = qualification.IsActive });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du toggle de la qualification prédéfinie {Id}", id);
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }

        // DELETE: api/staff/predefined-qualifications/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeletePredefinedQualification(Guid id)
        {
            try
            {
                var qualification = await _context.PredefinedQualifications
                    .Include(q => q.QualificationSectors)
                    .FirstOrDefaultAsync(q => q.Id == id);

                if (qualification == null)
                {
                    return NotFound(new { message = "Qualification prédéfinie introuvable" });
                }

                // Supprimer les associations d'abord (cascade devrait le faire, mais explicite pour clarté)
                _context.PredefinedQualificationSectors.RemoveRange(qualification.QualificationSectors);
                _context.PredefinedQualifications.Remove(qualification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Qualification prédéfinie supprimée: {Name} ({Id})", qualification.Name, id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression de la qualification prédéfinie {Id}", id);
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }

        // GET: api/staff/predefined-qualifications/types
        [HttpGet("types")]
        public ActionResult<IEnumerable<QualificationTypeDto>> GetQualificationTypes()
        {
            var types = Enum.GetValues<QualificationType>()
                .Select(t => new QualificationTypeDto
                {
                    Value = (int)t,
                    Name = t.ToString(),
                    Label = GetTypeLabel(t)
                })
                .ToList();

            return Ok(types);
        }

        private static string GetTypeLabel(QualificationType type)
        {
            return type switch
            {
                QualificationType.Custom => "Personnalisée",
                QualificationType.RNCP => "RNCP",
                QualificationType.RS => "Répertoire Spécifique",
                QualificationType.CQP => "CQP",
                QualificationType.Habilitation => "Habilitation",
                _ => type.ToString()
            };
        }
    }

    // DTOs
    public class PredefinedQualificationDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public QualificationType Type { get; set; }
        public string TypeLabel { get; set; } = string.Empty;
        public string? RncpCode { get; set; }
        public string? RsCode { get; set; }
        public string? FranceCompetencesUrl { get; set; }
        public int? Level { get; set; }
        public string? Certificateur { get; set; }
        public DateTime? DateEnregistrement { get; set; }
        public DateTime? DateFinValidite { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<PredefinedQualificationSectorDto> Sectors { get; set; } = new();
    }

    public class PredefinedQualificationSectorDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
    }

    public class CreatePredefinedQualificationRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public QualificationType Type { get; set; } = QualificationType.Custom;
        public string? RncpCode { get; set; }
        public string? RsCode { get; set; }
        public string? FranceCompetencesUrl { get; set; }
        public int? Level { get; set; }
        public string? Certificateur { get; set; }
        public DateTime? DateEnregistrement { get; set; }
        public DateTime? DateFinValidite { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; } = 0;
        public List<Guid>? SectorIds { get; set; }
    }

    public class UpdatePredefinedQualificationRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public QualificationType Type { get; set; }
        public string? RncpCode { get; set; }
        public string? RsCode { get; set; }
        public string? FranceCompetencesUrl { get; set; }
        public int? Level { get; set; }
        public string? Certificateur { get; set; }
        public DateTime? DateEnregistrement { get; set; }
        public DateTime? DateFinValidite { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; }
        public List<Guid>? SectorIds { get; set; }
    }

    public class QualificationTypeDto
    {
        public int Value { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Label { get; set; } = string.Empty;
    }
}
