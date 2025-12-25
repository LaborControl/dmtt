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
    public class IndustriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public IndustriesController(ApplicationDbContext context)
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

        // GET: api/industries
        [HttpGet]
        public async Task<ActionResult<IEnumerable<IndustryDto>>> GetIndustries(
            [FromQuery] Guid? sectorId = null,
            [FromQuery] bool? isActive = null)
        {
            try
            {
                var customerId = GetCustomerId();

                var query = _context.Industries
                    .Include(i => i.Sector)
                    .Where(i => i.CustomerId == customerId);

                if (sectorId.HasValue)
                {
                    query = query.Where(i => i.SectorId == sectorId.Value);
                }

                if (isActive.HasValue)
                {
                    query = query.Where(i => i.IsActive == isActive.Value);
                }

                var industries = await query
                    .OrderBy(i => i.DisplayOrder)
                    .ThenBy(i => i.Name)
                    .Select(i => new IndustryDto
                    {
                        Id = i.Id,
                        SectorId = i.SectorId,
                        SectorName = i.Sector!.Name,
                        SectorColor = i.Sector.Color,
                        SectorIcon = i.Sector.Icon,
                        Name = i.Name,
                        Code = i.Code,
                        Description = i.Description,
                        Color = i.Color,
                        Icon = i.Icon,
                        DisplayOrder = i.DisplayOrder,
                        RecommendedQualifications = i.RecommendedQualifications,
                        IsPredefined = i.IsPredefined,
                        IsActive = i.IsActive,
                        CreatedAt = i.CreatedAt,
                        QualificationsCount = _context.Qualifications.Count(q => q.SectorId == i.SectorId && q.CustomerId == customerId)
                    })
                    .ToListAsync();

                return Ok(industries);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur lors de la récupération des métiers: {ex.Message}");
            }
        }

        // GET: api/industries/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<IndustryDto>> GetIndustry(Guid id)
        {
            try
            {
                var customerId = GetCustomerId();

                var industry = await _context.Industries
                    .Include(i => i.Sector)
                    .Where(i => i.Id == id && i.CustomerId == customerId)
                    .Select(i => new IndustryDto
                    {
                        Id = i.Id,
                        SectorId = i.SectorId,
                        SectorName = i.Sector!.Name,
                        SectorColor = i.Sector.Color,
                        SectorIcon = i.Sector.Icon,
                        Name = i.Name,
                        Code = i.Code,
                        Description = i.Description,
                        Color = i.Color,
                        Icon = i.Icon,
                        DisplayOrder = i.DisplayOrder,
                        RecommendedQualifications = i.RecommendedQualifications,
                        IsPredefined = i.IsPredefined,
                        IsActive = i.IsActive,
                        CreatedAt = i.CreatedAt
                    })
                    .FirstOrDefaultAsync();

                if (industry == null)
                {
                    return NotFound("Métier non trouvé");
                }

                return Ok(industry);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur lors de la récupération du métier: {ex.Message}");
            }
        }

        // GET: api/industries/validate-sectors
        // Vérifie que tous les secteurs actifs ont au moins un métier actif
        [HttpGet("validate-sectors")]
        public async Task<ActionResult> ValidateSectors()
        {
            try
            {
                var customerId = GetCustomerId();

                // Récupérer tous les secteurs actifs
                var activeSectors = await _context.Sectors
                    .Where(s => s.CustomerId == customerId && s.IsActive)
                    .ToListAsync();

                // Pour chaque secteur, vérifier s'il a au moins un métier actif
                var sectorsWithoutIndustries = new List<SectorValidationDto>();

                foreach (var sector in activeSectors)
                {
                    var hasActiveIndustries = await _context.Industries
                        .AnyAsync(i => i.SectorId == sector.Id &&
                                      i.CustomerId == customerId &&
                                      i.IsActive);

                    if (!hasActiveIndustries)
                    {
                        sectorsWithoutIndustries.Add(new SectorValidationDto
                        {
                            Id = sector.Id,
                            Name = sector.Name,
                            Code = sector.Code,
                            Icon = sector.Icon,
                            Color = sector.Color
                        });
                    }
                }

                if (sectorsWithoutIndustries.Any())
                {
                    return Ok(new ValidationResult
                    {
                        IsValid = false,
                        Message = $"{sectorsWithoutIndustries.Count} secteur(s) sans métier actif",
                        SectorsWithoutIndustries = sectorsWithoutIndustries
                    });
                }

                return Ok(new ValidationResult
                {
                    IsValid = true,
                    Message = "Tous les secteurs actifs ont au moins un métier actif",
                    SectorsWithoutIndustries = new List<SectorValidationDto>()
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur lors de la validation des secteurs: {ex.Message}");
            }
        }

        // POST: api/industries
        [HttpPost]
        public async Task<ActionResult<IndustryDto>> CreateIndustry([FromBody] CreateIndustryRequest request)
        {
            try
            {
                var customerId = GetCustomerId();

                // Vérifier que le secteur existe
                var sectorExists = await _context.Sectors
                    .AnyAsync(s => s.Id == request.SectorId && s.CustomerId == customerId && s.IsActive);

                if (!sectorExists)
                {
                    return BadRequest("Le secteur spécifié n'existe pas");
                }

                // Vérifier si le nom existe déjà dans ce secteur
                var exists = await _context.Industries
                    .AnyAsync(i => i.CustomerId == customerId &&
                                   i.SectorId == request.SectorId &&
                                   i.Name == request.Name &&
                                   i.IsActive);

                if (exists)
                {
                    return BadRequest("Un métier avec ce nom existe déjà dans ce secteur");
                }

                var industry = new Industry
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    SectorId = request.SectorId,
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

                _context.Industries.Add(industry);
                await _context.SaveChangesAsync();

                // Récupérer le secteur pour le DTO
                var sector = await _context.Sectors.FindAsync(request.SectorId);

                var dto = new IndustryDto
                {
                    Id = industry.Id,
                    SectorId = industry.SectorId,
                    SectorName = sector?.Name ?? "",
                    SectorColor = sector?.Color,
                    SectorIcon = sector?.Icon,
                    Name = industry.Name,
                    Code = industry.Code,
                    Description = industry.Description,
                    Color = industry.Color,
                    Icon = industry.Icon,
                    DisplayOrder = industry.DisplayOrder,
                    RecommendedQualifications = industry.RecommendedQualifications,
                    IsPredefined = industry.IsPredefined,
                    IsActive = industry.IsActive,
                    CreatedAt = industry.CreatedAt
                };

                return CreatedAtAction(nameof(GetIndustry), new { id = industry.Id }, dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur lors de la création du métier: {ex.Message}");
            }
        }

        // PUT: api/industries/{id}
        [HttpPut("{id}")]
        public async Task<ActionResult<IndustryDto>> UpdateIndustry(Guid id, [FromBody] UpdateIndustryRequest request)
        {
            try
            {
                var customerId = GetCustomerId();

                var industry = await _context.Industries
                    .Include(i => i.Sector)
                    .FirstOrDefaultAsync(i => i.Id == id && i.CustomerId == customerId);

                if (industry == null)
                {
                    return NotFound("Métier non trouvé");
                }

                // Si le secteur change, vérifier qu'il existe
                if (request.SectorId != industry.SectorId)
                {
                    var sectorExists = await _context.Sectors
                        .AnyAsync(s => s.Id == request.SectorId && s.CustomerId == customerId && s.IsActive);

                    if (!sectorExists)
                    {
                        return BadRequest("Le secteur spécifié n'existe pas");
                    }
                }

                // Vérifier si le nouveau nom existe déjà dans ce secteur (sauf pour ce métier)
                var nameExists = await _context.Industries
                    .AnyAsync(i => i.CustomerId == customerId &&
                                   i.SectorId == request.SectorId &&
                                   i.Name == request.Name &&
                                   i.Id != id &&
                                   i.IsActive);

                if (nameExists)
                {
                    return BadRequest("Un métier avec ce nom existe déjà dans ce secteur");
                }

                industry.SectorId = request.SectorId;
                industry.Name = request.Name;
                industry.Code = request.Code;
                industry.Description = request.Description;
                industry.Color = request.Color;
                industry.Icon = request.Icon;
                industry.DisplayOrder = request.DisplayOrder;
                industry.RecommendedQualifications = request.RecommendedQualifications;
                industry.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                // Recharger le secteur si changé
                if (request.SectorId != industry.Sector?.Id)
                {
                    industry.Sector = await _context.Sectors.FindAsync(request.SectorId);
                }

                var dto = new IndustryDto
                {
                    Id = industry.Id,
                    SectorId = industry.SectorId,
                    SectorName = industry.Sector?.Name ?? "",
                    SectorColor = industry.Sector?.Color,
                    SectorIcon = industry.Sector?.Icon,
                    Name = industry.Name,
                    Code = industry.Code,
                    Description = industry.Description,
                    Color = industry.Color,
                    Icon = industry.Icon,
                    DisplayOrder = industry.DisplayOrder,
                    RecommendedQualifications = industry.RecommendedQualifications,
                    IsPredefined = industry.IsPredefined,
                    IsActive = industry.IsActive,
                    CreatedAt = industry.CreatedAt
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur lors de la mise à jour du métier: {ex.Message}");
            }
        }

        // DELETE: api/industries/{id}
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteIndustry(Guid id)
        {
            try
            {
                var customerId = GetCustomerId();

                var industry = await _context.Industries
                    .FirstOrDefaultAsync(i => i.Id == id && i.CustomerId == customerId);

                if (industry == null)
                {
                    return NotFound("Métier non trouvé");
                }

                // Soft delete
                industry.IsActive = false;
                industry.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return Ok(new { message = "Métier supprimé avec succès" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur lors de la suppression du métier: {ex.Message}");
            }
        }

        // POST: api/industries/{id}/toggle
        /// <summary>
        /// Active ou désactive un métier (toggle IsActive)
        /// Si activation et métier prédéfini: initialise automatiquement les qualifications prédéfinies
        /// </summary>
        [HttpPost("{id}/toggle")]
        public async Task<ActionResult<IndustryDto>> ToggleIndustry(Guid id)
        {
            try
            {
                var customerId = GetCustomerId();

                var industry = await _context.Industries
                    .Include(i => i.Sector)
                    .FirstOrDefaultAsync(i => i.Id == id && i.CustomerId == customerId);

                if (industry == null)
                {
                    return NotFound("Métier non trouvé");
                }

                // Toggle IsActive
                industry.IsActive = !industry.IsActive;
                industry.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                var dto = new IndustryDto
                {
                    Id = industry.Id,
                    SectorId = industry.SectorId,
                    SectorName = industry.Sector?.Name ?? "",
                    SectorColor = industry.Sector?.Color,
                    SectorIcon = industry.Sector?.Icon,
                    Name = industry.Name,
                    Code = industry.Code,
                    Description = industry.Description,
                    Color = industry.Color,
                    Icon = industry.Icon,
                    DisplayOrder = industry.DisplayOrder,
                    RecommendedQualifications = industry.RecommendedQualifications,
                    IsPredefined = industry.IsPredefined,
                    IsActive = industry.IsActive,
                    CreatedAt = industry.CreatedAt,
                    QualificationsCount = _context.Qualifications.Count(q => q.SectorId == industry.SectorId && q.CustomerId == customerId)
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur lors du toggle du métier: {ex.Message}");
            }
        }

        // POST: api/industries/init-predefined/{sectorId}
        /// <summary>
        /// Initialise les métiers prédéfinis pour un secteur donné (inactifs par défaut)
        /// </summary>
        [HttpPost("init-predefined/{sectorId}")]
        public async Task<ActionResult> InitPredefinedIndustries(Guid sectorId)
        {
            try
            {
                var customerId = GetCustomerId();

                // Vérifier que le secteur existe et est actif
                var sector = await _context.Sectors
                    .FirstOrDefaultAsync(s => s.Id == sectorId && s.CustomerId == customerId && s.IsActive);

                if (sector == null)
                {
                    return BadRequest("Le secteur spécifié n'existe pas ou n'est pas actif");
                }

                // Vérifier si déjà initialisé pour ce secteur
                var existingCount = await _context.Industries
                    .CountAsync(i => i.CustomerId == customerId && i.SectorId == sectorId && i.IsPredefined);

                if (existingCount > 0)
                {
                    return BadRequest($"Les métiers prédéfinis ont déjà été initialisés pour ce secteur ({existingCount} métiers trouvés)");
                }

                // Créer les métiers prédéfinis selon le secteur
                var predefinedIndustries = GetPredefinedIndustriesForSector(sector.Code ?? sector.Name, customerId, sectorId);

                if (predefinedIndustries.Count == 0)
                {
                    return BadRequest("Aucun métier prédéfini disponible pour ce secteur");
                }

                _context.Industries.AddRange(predefinedIndustries);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"{predefinedIndustries.Count} métiers prédéfinis initialisés avec succès (tous inactifs par défaut)", count = predefinedIndustries.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Erreur lors de l'initialisation des métiers prédéfinis: {ex.Message}");
            }
        }

        private List<Industry> GetPredefinedIndustriesForSector(string sectorCode, Guid customerId, Guid sectorId)
        {
            var industries = new List<Industry>();
            var baseDate = DateTime.UtcNow;

            switch (sectorCode.ToUpper())
            {
                case "MAINTENANCE":
                    industries.AddRange(new[]
                    {
                        CreatePredefinedIndustry("Technicien de maintenance", "TECH_MAINT", "Maintenance préventive et curative des équipements", customerId, sectorId, 1),
                        CreatePredefinedIndustry("Électricien industriel", "ELEC", "Installation et maintenance électrique", customerId, sectorId, 2),
                        CreatePredefinedIndustry("Mécanicien industriel", "MECA", "Réparation et entretien mécanique", customerId, sectorId, 3),
                        CreatePredefinedIndustry("Automaticien", "AUTO", "Maintenance des systèmes automatisés", customerId, sectorId, 4),
                        CreatePredefinedIndustry("Frigoriste", "FRIGO", "Maintenance des installations frigorifiques", customerId, sectorId, 5),
                        CreatePredefinedIndustry("Hydraulicien", "HYDRAU", "Systèmes hydrauliques et pneumatiques", customerId, sectorId, 6)
                    });
                    break;

                case "QHSE":
                    industries.AddRange(new[]
                    {
                        CreatePredefinedIndustry("Responsable QHSE", "RESP_QHSE", "Management qualité, hygiène, sécurité, environnement", customerId, sectorId, 1),
                        CreatePredefinedIndustry("Animateur sécurité", "ANIM_SEC", "Animation et sensibilisation sécurité", customerId, sectorId, 2),
                        CreatePredefinedIndustry("Auditeur qualité", "AUDIT_Q", "Audits et contrôles qualité", customerId, sectorId, 3),
                        CreatePredefinedIndustry("Technicien HSE", "TECH_HSE", "Prévention des risques professionnels", customerId, sectorId, 4)
                    });
                    break;

                case "SANTE":
                    industries.AddRange(new[]
                    {
                        CreatePredefinedIndustry("Aide-soignant", "AS", "Soins et accompagnement des patients", customerId, sectorId, 1),
                        CreatePredefinedIndustry("Infirmier", "IDE", "Soins infirmiers", customerId, sectorId, 2),
                        CreatePredefinedIndustry("Auxiliaire de vie", "AVS", "Aide à domicile", customerId, sectorId, 3),
                        CreatePredefinedIndustry("Agent de service hospitalier", "ASH", "Entretien et hygiène hospitalière", customerId, sectorId, 4)
                    });
                    break;

                case "NETTOYAGE":
                    industries.AddRange(new[]
                    {
                        CreatePredefinedIndustry("Agent de nettoyage", "AGENT_NET", "Nettoyage et entretien des locaux", customerId, sectorId, 1),
                        CreatePredefinedIndustry("Agent de propreté urbaine", "APU", "Nettoyage espaces publics", customerId, sectorId, 2),
                        CreatePredefinedIndustry("Chef d'équipe propreté", "CHEF_NET", "Encadrement équipe nettoyage", customerId, sectorId, 3)
                    });
                    break;

                case "SECURITE":
                    industries.AddRange(new[]
                    {
                        CreatePredefinedIndustry("Agent de sécurité", "ADS", "Surveillance et sécurisation des sites", customerId, sectorId, 1),
                        CreatePredefinedIndustry("Agent cynophile", "CYNO", "Sécurité avec chien", customerId, sectorId, 2),
                        CreatePredefinedIndustry("Agent de sécurité incendie", "SSIAP", "Prévention et intervention incendie", customerId, sectorId, 3),
                        CreatePredefinedIndustry("Rondier-intervenant", "RONDIER", "Rondes de sécurité", customerId, sectorId, 4)
                    });
                    break;

                case "COMMERCE":
                    industries.AddRange(new[]
                    {
                        CreatePredefinedIndustry("Vendeur", "VENDEUR", "Vente et conseil client", customerId, sectorId, 1),
                        CreatePredefinedIndustry("Caissier", "CAISSIER", "Encaissement", customerId, sectorId, 2),
                        CreatePredefinedIndustry("Employé commercial", "EMP_COM", "Mise en rayon et conseil", customerId, sectorId, 3),
                        CreatePredefinedIndustry("Chef de rayon", "CHEF_RAYON", "Gestion de rayon", customerId, sectorId, 4)
                    });
                    break;

                case "RESTAURATION":
                    industries.AddRange(new[]
                    {
                        CreatePredefinedIndustry("Cuisinier", "CUISINIER", "Préparation des plats", customerId, sectorId, 1),
                        CreatePredefinedIndustry("Serveur", "SERVEUR", "Service en salle", customerId, sectorId, 2),
                        CreatePredefinedIndustry("Commis de cuisine", "COMMIS", "Aide en cuisine", customerId, sectorId, 3),
                        CreatePredefinedIndustry("Plongeur", "PLONGEUR", "Plonge et entretien", customerId, sectorId, 4)
                    });
                    break;

                case "LOGISTIQUE":
                    industries.AddRange(new[]
                    {
                        CreatePredefinedIndustry("Cariste", "CARISTE", "Conduite de chariots élévateurs", customerId, sectorId, 1),
                        CreatePredefinedIndustry("Préparateur de commandes", "PREP_CMD", "Préparation et expédition", customerId, sectorId, 2),
                        CreatePredefinedIndustry("Magasinier", "MAGASINIER", "Gestion des stocks", customerId, sectorId, 3),
                        CreatePredefinedIndustry("Agent de quai", "AGENT_QUAI", "Chargement/déchargement", customerId, sectorId, 4)
                    });
                    break;

                case "BTP":
                    industries.AddRange(new[]
                    {
                        CreatePredefinedIndustry("Maçon", "MACON", "Travaux de maçonnerie", customerId, sectorId, 1),
                        CreatePredefinedIndustry("Électricien bâtiment", "ELEC_BAT", "Installations électriques", customerId, sectorId, 2),
                        CreatePredefinedIndustry("Plombier", "PLOMBIER", "Installation sanitaire", customerId, sectorId, 3),
                        CreatePredefinedIndustry("Peintre en bâtiment", "PEINTRE", "Travaux de peinture", customerId, sectorId, 4),
                        CreatePredefinedIndustry("Charpentier", "CHARPENTIER", "Construction charpente", customerId, sectorId, 5)
                    });
                    break;

                case "IT":
                    industries.AddRange(new[]
                    {
                        CreatePredefinedIndustry("Technicien support", "TECH_SUPPORT", "Support utilisateurs", customerId, sectorId, 1),
                        CreatePredefinedIndustry("Administrateur système", "ADMIN_SYS", "Gestion infrastructure IT", customerId, sectorId, 2),
                        CreatePredefinedIndustry("Développeur", "DEV", "Développement logiciel", customerId, sectorId, 3),
                        CreatePredefinedIndustry("Technicien réseau", "TECH_RESEAU", "Administration réseaux", customerId, sectorId, 4)
                    });
                    break;
            }

            return industries;
        }

        private Industry CreatePredefinedIndustry(string name, string code, string description, Guid customerId, Guid sectorId, int displayOrder)
        {
            return new Industry
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                SectorId = sectorId,
                Name = name,
                Code = code,
                Description = description,
                DisplayOrder = displayOrder,
                IsPredefined = true,
                IsActive = false,
                CreatedAt = DateTime.UtcNow
            };
        }
    }

    // DTOs
    public class IndustryDto
    {
        public Guid Id { get; set; }
        public Guid SectorId { get; set; }
        public string SectorName { get; set; } = string.Empty;
        public string? SectorColor { get; set; }
        public string? SectorIcon { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; }
        public string? RecommendedQualifications { get; set; }
        public bool IsPredefined { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int QualificationsCount { get; set; }
    }

    public class CreateIndustryRequest
    {
        public Guid SectorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; } = 0;
        public string? RecommendedQualifications { get; set; }
    }

    public class UpdateIndustryRequest
    {
        public Guid SectorId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; }
        public string? RecommendedQualifications { get; set; }
    }

    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public List<SectorValidationDto> SectorsWithoutIndustries { get; set; } = new();
    }

    public class SectorValidationDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Code { get; set; }
        public string? Icon { get; set; }
        public string? Color { get; set; }
    }
}
