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
    public class TaskTemplatesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public TaskTemplatesController(ApplicationDbContext context)
        {
            _context = context;
        }

        private Guid GetCustomerId()
        {
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;
            return Guid.Parse(customerIdClaim!);
        }

        // GET: api/tasktemplates
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetTemplates([FromQuery] Guid? industryId = null)
        {
            var customerId = GetCustomerId();

            var query = _context.TaskTemplates
                .Include(t => t.Industry)
                .ThenInclude(i => i!.Sector)
                .Include(t => t.TaskTemplateQualifications)
                .ThenInclude(ttq => ttq.Qualification)
                .Where(t => t.CustomerId == customerId);

            if (industryId.HasValue)
            {
                query = query.Where(t => t.IndustryId == industryId.Value);
            }

            var templates = await query
                .OrderBy(t => t.Name)
                .ToListAsync();

            // Map to include QualificationIds
            var result = templates.Select(t => new
            {
                t.Id,
                t.Name,
                t.Category,
                t.LegalWarning,
                t.FormTemplate,
                t.IsPredefined,
                t.IsActive,
                t.RequireDoubleScan,
                t.IndustryId,
                IndustryName = t.Industry?.Name,
                IndustryIcon = t.Industry?.Icon,
                t.CreatedAt,
                QualificationIds = t.TaskTemplateQualifications.Select(ttq => ttq.QualificationId).ToList()
            });

            return Ok(result);
        }

        // GET: api/tasktemplates/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<object>> GetTemplate(Guid id)
        {
            var customerId = GetCustomerId();

            var template = await _context.TaskTemplates
                .Include(t => t.TaskTemplateQualifications)
                .ThenInclude(ttq => ttq.Qualification)
                .FirstOrDefaultAsync(t => t.Id == id && t.CustomerId == customerId && t.IsActive);

            if (template == null)
                return NotFound();

            var result = new
            {
                template.Id,
                template.Name,
                template.Category,
                template.LegalWarning,
                template.FormTemplate,
                template.IsPredefined,
                template.IsActive,
                template.RequireDoubleScan,
                template.IndustryId,
                template.CreatedAt,
                QualificationIds = template.TaskTemplateQualifications.Select(ttq => ttq.QualificationId).ToList()
            };

            return Ok(result);
        }

        // POST: api/tasktemplates
        [HttpPost]
        public async Task<ActionResult<object>> CreateTemplate([FromBody] TaskTemplateRequest request)
        {
            var customerId = GetCustomerId();

            var template = new TaskTemplate
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                Name = request.Name,
                Category = request.Category,
                LegalWarning = request.LegalWarning,
                FormTemplate = request.FormTemplate,
                IndustryId = request.IndustryId,
                IsUniversal = request.IsUniversal,
                AlertOnMismatch = request.AlertOnMismatch,
                RequireDoubleScan = request.RequireDoubleScan,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.TaskTemplates.Add(template);

            // Add qualifications if provided
            if (request.QualificationIds != null && request.QualificationIds.Any())
            {
                foreach (var qualificationId in request.QualificationIds)
                {
                    var ttq = new TaskTemplateQualification
                    {
                        Id = Guid.NewGuid(),
                        TaskTemplateId = template.Id,
                        QualificationId = qualificationId,
                        IsMandatory = true,
                        AlertLevel = 3,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.TaskTemplateQualifications.Add(ttq);
                }
            }

            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetTemplate), new { id = template.Id }, new { Id = template.Id });
        }

        // PUT: api/tasktemplates/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateTemplate(Guid id, [FromBody] TaskTemplateRequest request)
        {
            var customerId = GetCustomerId();

            var existingTemplate = await _context.TaskTemplates
                .Include(t => t.TaskTemplateQualifications)
                .FirstOrDefaultAsync(t => t.Id == id && t.CustomerId == customerId);

            if (existingTemplate == null)
                return NotFound();

            existingTemplate.Name = request.Name;
            existingTemplate.Category = request.Category;
            existingTemplate.IsUniversal = request.IsUniversal;
            existingTemplate.AlertOnMismatch = request.AlertOnMismatch;
            existingTemplate.LegalWarning = request.LegalWarning;
            existingTemplate.FormTemplate = request.FormTemplate;
            existingTemplate.RequireDoubleScan = request.RequireDoubleScan;
            existingTemplate.UpdatedAt = DateTime.UtcNow;

            // Update qualifications: remove old ones and add new ones
            var existingQualifications = existingTemplate.TaskTemplateQualifications.ToList();
            _context.TaskTemplateQualifications.RemoveRange(existingQualifications);

            if (request.QualificationIds != null && request.QualificationIds.Any())
            {
                foreach (var qualificationId in request.QualificationIds)
                {
                    var ttq = new TaskTemplateQualification
                    {
                        Id = Guid.NewGuid(),
                        TaskTemplateId = existingTemplate.Id,
                        QualificationId = qualificationId,
                        IsMandatory = true,
                        AlertLevel = 3,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.TaskTemplateQualifications.Add(ttq);
                }
            }

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/tasktemplates/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteTemplate(Guid id)
        {
            var customerId = GetCustomerId();

            var template = await _context.TaskTemplates
                .FirstOrDefaultAsync(t => t.Id == id && t.CustomerId == customerId);

            if (template == null)
                return NotFound();

            // Soft delete
            template.IsActive = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpPost("seed-ehpad")]
        public async Task<ActionResult> SeedEhpadTemplates()
        {
            // Récupérer le premier client (EHPAD Les Roses)
            var customer = await _context.Customers.FirstOrDefaultAsync();
            if (customer == null)
            {
                return BadRequest("Aucun client trouvé. Créez d'abord un client.");
            }

            var templates = new List<TaskTemplate>
            {
                // Templates UNIVERSELS (tous peuvent faire)
                new TaskTemplate
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    Name = "Vérification présence résident",
                    Category = "SURVEILLANCE",
                    IsUniversal = true,
                    AlertOnMismatch = false,
                    FormTemplate = @"{
                        ""fields"": [
                            {""name"": ""resident_present"", ""type"": ""boolean"", ""label"": ""Résident présent dans la chambre?""},
                            {""name"": ""etat_general"", ""type"": ""select"", ""label"": ""État général"", ""options"": [""Bon"", ""Moyen"", ""Préoccupant""]},
                            {""name"": ""observations"", ""type"": ""text"", ""label"": ""Observations"", ""required"": false}
                        ]
                    }"
                },
                new TaskTemplate
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    Name = "Signalement anomalie urgente",
                    Category = "SURVEILLANCE",
                    IsUniversal = true,
                    AlertOnMismatch = false,
                    FormTemplate = @"{
                        ""fields"": [
                            {""name"": ""type_anomalie"", ""type"": ""select"", ""label"": ""Type"", ""options"": [""Chute"", ""Malaise"", ""Agitation"", ""Fugue"", ""Autre""]},
                            {""name"": ""urgence"", ""type"": ""select"", ""label"": ""Urgence"", ""options"": [""Immédiate"", ""Dans l'heure"", ""Dans la journée""]},
                            {""name"": ""description"", ""type"": ""text"", ""label"": ""Description""}
                        ]
                    }"
                },

                // Templates ASH
                new TaskTemplate
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    Name = "Nettoyage chambre",
                    Category = "HOTELLERIE",
                    IsUniversal = false,
                    AlertOnMismatch = false,
                    FormTemplate = @"{
                        ""fields"": [
                            {""name"": ""chambre_nettoyee"", ""type"": ""boolean"", ""label"": ""Chambre nettoyée?""},
                            {""name"": ""lit_fait"", ""type"": ""boolean"", ""label"": ""Lit fait?""},
                            {""name"": ""sanitaires"", ""type"": ""boolean"", ""label"": ""Sanitaires nettoyés?""},
                            {""name"": ""poubelles"", ""type"": ""boolean"", ""label"": ""Poubelles vidées?""},
                            {""name"": ""remarques"", ""type"": ""text"", ""label"": ""Remarques"", ""required"": false}
                        ]
                    }"
                },
                new TaskTemplate
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    Name = "Distribution repas",
                    Category = "HOTELLERIE",
                    IsUniversal = false,
                    AlertOnMismatch = false,
                    FormTemplate = @"{
                        ""fields"": [
                            {""name"": ""repas_servi"", ""type"": ""boolean"", ""label"": ""Repas servi?""},
                            {""name"": ""quantite_mangee"", ""type"": ""select"", ""label"": ""Quantité mangée"", ""options"": [""Rien"", ""Peu"", ""Moitié"", ""Tout""]},
                            {""name"": ""aide_necessaire"", ""type"": ""boolean"", ""label"": ""Aide nécessaire?""},
                            {""name"": ""observations"", ""type"": ""text"", ""label"": ""Observations"", ""required"": false}
                        ]
                    }"
                },

                // Templates AS (Aide-Soignant)
                new TaskTemplate
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    Name = "Toilette complète",
                    Category = "SOIN_BASE",
                    IsUniversal = false,
                    AlertOnMismatch = true,
                    LegalWarning = "Acte réservé aux AS diplômés. Glissement de tâche tracé.",
                    FormTemplate = @"{
                        ""fields"": [
                            {""name"": ""toilette_effectuee"", ""type"": ""select"", ""label"": ""Toilette"", ""options"": [""Complète"", ""Partielle"", ""Refus""]},
                            {""name"": ""douche_bain"", ""type"": ""select"", ""label"": ""Type"", ""options"": [""Douche"", ""Bain"", ""Au lit""]},
                            {""name"": ""etat_cutane"", ""type"": ""select"", ""label"": ""État cutané"", ""options"": [""Normal"", ""Rougeurs"", ""Escarre""]},
                            {""name"": ""participation"", ""type"": ""select"", ""label"": ""Participation résident"", ""options"": [""Active"", ""Partielle"", ""Passive""]},
                            {""name"": ""transmissions"", ""type"": ""text"", ""label"": ""Transmissions IDE"", ""required"": false}
                        ]
                    }"
                },
                new TaskTemplate
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    Name = "Change protection",
                    Category = "SOIN_BASE",
                    IsUniversal = false,
                    AlertOnMismatch = true,
                    LegalWarning = "Acte de soin réservé aux AS diplômés.",
                    FormTemplate = @"{
                        ""fields"": [
                            {""name"": ""change_effectue"", ""type"": ""boolean"", ""label"": ""Change effectué?""},
                            {""name"": ""etat"", ""type"": ""select"", ""label"": ""État"", ""options"": [""Sec"", ""Humide"", ""Souillé""]},
                            {""name"": ""etat_cutane"", ""type"": ""select"", ""label"": ""État cutané"", ""options"": [""Normal"", ""Irrité"", ""Lésé""]},
                            {""name"": ""observations"", ""type"": ""text"", ""label"": ""Observations"", ""required"": false}
                        ]
                    }"
                },

                // Templates IDE (Infirmier)
                new TaskTemplate
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    Name = "Distribution médicaments",
                    Category = "SOIN_TECHNIQUE",
                    IsUniversal = false,
                    AlertOnMismatch = true,
                    LegalWarning = "ACTE INFIRMIER - Distribution médicaments réservée aux IDE diplômés.",
                    FormTemplate = @"{
                        ""fields"": [
                            {""name"": ""medicaments_donnes"", ""type"": ""select"", ""label"": ""Médicaments"", ""options"": [""Tous pris"", ""Partiellement pris"", ""Refus""]},
                            {""name"": ""horaire"", ""type"": ""select"", ""label"": ""Moment"", ""options"": [""Matin"", ""Midi"", ""Soir"", ""Coucher""]},
                            {""name"": ""probleme"", ""type"": ""boolean"", ""label"": ""Problème détecté?""},
                            {""name"": ""transmissions"", ""type"": ""text"", ""label"": ""Transmissions"", ""required"": false}
                        ]
                    }"
                },
                new TaskTemplate
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    Name = "Surveillance paramètres vitaux",
                    Category = "SOIN_TECHNIQUE",
                    IsUniversal = false,
                    AlertOnMismatch = true,
                    LegalWarning = "Surveillance médicale réservée aux IDE.",
                    FormTemplate = @"{
                        ""fields"": [
                            {""name"": ""temperature"", ""type"": ""number"", ""label"": ""Température (°C)"", ""min"": 35, ""max"": 42},
                            {""name"": ""tension_sys"", ""type"": ""number"", ""label"": ""Tension systolique"", ""min"": 70, ""max"": 200},
                            {""name"": ""tension_dia"", ""type"": ""number"", ""label"": ""Tension diastolique"", ""min"": 40, ""max"": 130},
                            {""name"": ""pouls"", ""type"": ""number"", ""label"": ""Pouls"", ""min"": 40, ""max"": 150},
                            {""name"": ""saturation"", ""type"": ""number"", ""label"": ""Saturation O2"", ""min"": 70, ""max"": 100},
                            {""name"": ""alerte_medecin"", ""type"": ""boolean"", ""label"": ""Alerter médecin?""}
                        ]
                    }"
                },

                // Templates TECH (Maintenance EHPAD)
                new TaskTemplate
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customer.Id,
                    Name = "Intervention technique chambre",
                    Category = "TECHNIQUE",
                    IsUniversal = false,
                    AlertOnMismatch = false,
                    FormTemplate = @"{
                        ""fields"": [
                            {""name"": ""type_intervention"", ""type"": ""select"", ""label"": ""Type"", ""options"": [""Plomberie"", ""Électricité"", ""Chauffage"", ""Mobilier"", ""Autre""]},
                            {""name"": ""probleme_resolu"", ""type"": ""select"", ""label"": ""Résolu?"", ""options"": [""Oui"", ""Non"", ""Temporaire""]},
                            {""name"": ""pieces_changees"", ""type"": ""text"", ""label"": ""Pièces changées"", ""required"": false},
                            {""name"": ""intervention_externe"", ""type"": ""boolean"", ""label"": ""Intervention externe nécessaire?""},
                            {""name"": ""photo_avant"", ""type"": ""photo"", ""label"": ""Photo avant"", ""required"": false},
                            {""name"": ""photo_apres"", ""type"": ""photo"", ""label"": ""Photo après"", ""required"": false}
                        ]
                    }"
                }
            };

            _context.TaskTemplates.AddRange(templates);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"{templates.Count} templates EHPAD créés avec succès!" });
        }

        [HttpGet("by-qualification/{qualification}")]
        public async Task<ActionResult<IEnumerable<TaskTemplate>>> GetTemplatesByQualification(string qualification)
        {
            var templates = await _context.TaskTemplates
                .Where(t => t.IsActive && t.IsUniversal)
                .OrderBy(t => t.Category)
                .ThenBy(t => t.Name)
                .ToListAsync();

            return Ok(templates);
        }

        // POST: api/tasktemplates/{id}/toggle
        [HttpPost("{id}/toggle")]
        public async Task<ActionResult<TaskTemplate>> ToggleTaskTemplate(Guid id)
        {
            var customerId = GetCustomerId();

            var template = await _context.TaskTemplates
                .Include(t => t.Industry)
                .ThenInclude(i => i!.Sector)
                .FirstOrDefaultAsync(t => t.Id == id && t.CustomerId == customerId);

            if (template == null)
                return NotFound("Protocole non trouvé");

            // Toggle IsActive
            template.IsActive = !template.IsActive;
            template.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Ok(template);
        }

        // POST: api/tasktemplates/init-predefined/{industryId}
        [HttpPost("init-predefined/{industryId}")]
        public async Task<ActionResult> InitPredefinedTaskTemplates(Guid industryId)
        {
            var customerId = GetCustomerId();

            var industry = await _context.Industries
                .Include(i => i.Sector)
                .FirstOrDefaultAsync(i => i.Id == industryId && i.CustomerId == customerId && i.IsActive);

            if (industry == null)
                return BadRequest("Le métier spécifié n'existe pas ou n'est pas actif");

            var existingCount = await _context.TaskTemplates
                .CountAsync(t => t.CustomerId == customerId && t.IndustryId == industryId && t.IsPredefined);

            if (existingCount > 0)
                return BadRequest($"Les protocoles prédéfinis ont déjà été initialisés pour ce métier");

            var predefinedTemplates = GetPredefinedTaskTemplatesForIndustry(industry.Code ?? industry.Name, customerId, industryId);

            _context.TaskTemplates.AddRange(predefinedTemplates);
            await _context.SaveChangesAsync();

            return Ok(new { message = $"{predefinedTemplates.Count} protocoles prédéfinis initialisés avec succès", count = predefinedTemplates.Count });
        }

        private List<TaskTemplate> GetPredefinedTaskTemplatesForIndustry(string industryCode, Guid customerId, Guid industryId)
        {
            return PredefinedProtocols.GetPredefinedTaskTemplatesForIndustry(industryCode, customerId, industryId);
        }

        // POST: api/tasktemplates/init-all-predefined
        [HttpPost("init-all-predefined")]
        public async Task<ActionResult> InitAllPredefinedTaskTemplates()
        {
            var customerId = GetCustomerId();

            // Récupérer tous les métiers actifs sauf ceux du secteur "Maintenance industrielle"
            var industries = await _context.Industries
                .Include(i => i.Sector)
                .Where(i => i.CustomerId == customerId &&
                           i.IsActive &&
                           i.Sector != null &&
                           i.Sector.Name != "Maintenance industrielle")
                .ToListAsync();

            if (!industries.Any())
                return BadRequest("Aucun métier actif trouvé (hors Maintenance industrielle)");

            int totalProtocolsCreated = 0;
            var industriesProcessed = new List<string>();

            foreach (var industry in industries)
            {
                // Vérifier si les protocoles prédéfinis n'existent pas déjà pour ce métier
                var existingCount = await _context.TaskTemplates
                    .CountAsync(t => t.CustomerId == customerId && t.IndustryId == industry.Id && t.IsPredefined);

                if (existingCount == 0)
                {
                    var predefinedTemplates = GetPredefinedTaskTemplatesForIndustry(
                        industry.Code ?? industry.Name,
                        customerId,
                        industry.Id);

                    if (predefinedTemplates.Any())
                    {
                        _context.TaskTemplates.AddRange(predefinedTemplates);
                        totalProtocolsCreated += predefinedTemplates.Count;
                        industriesProcessed.Add($"{industry.Sector!.Icon} {industry.Sector.Name} - {industry.Icon} {industry.Name} ({predefinedTemplates.Count})");
                    }
                }
            }

            if (totalProtocolsCreated > 0)
            {
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                message = $"{totalProtocolsCreated} protocoles prédéfinis initialisés avec succès pour {industriesProcessed.Count} métiers",
                totalProtocols = totalProtocolsCreated,
                industriesCount = industriesProcessed.Count,
                industries = industriesProcessed
            });
        }

        // GET: api/tasktemplates/metadata/{industryCode}
        [HttpGet("metadata/{industryCode}")]
        public ActionResult GetMetadataForIndustry(string industryCode)
        {
            var allCategories = IndustryMetadata.GetCategoriesByIndustry();
            var allQualifications = IndustryMetadata.GetQualificationsByIndustry();

            var categories = allCategories.ContainsKey(industryCode.ToUpper())
                ? allCategories[industryCode.ToUpper()]
                : IndustryMetadata.GetDefaultCategories();

            var qualifications = allQualifications.ContainsKey(industryCode.ToUpper())
                ? allQualifications[industryCode.ToUpper()]
                : IndustryMetadata.GetDefaultQualifications();

            return Ok(new
            {
                categories,
                qualifications
            });
        }
    }

    // DTO for creating/updating task templates with qualifications
    public class TaskTemplateRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string? RequiredQualification { get; set; }
        public string? LegalWarning { get; set; }
        public string FormTemplate { get; set; } = "{}";
        public Guid? IndustryId { get; set; }
        public bool IsUniversal { get; set; } = false;
        public bool AlertOnMismatch { get; set; } = true;
        public bool RequireDoubleScan { get; set; } = false;
        public List<Guid>? QualificationIds { get; set; }
    }
}
