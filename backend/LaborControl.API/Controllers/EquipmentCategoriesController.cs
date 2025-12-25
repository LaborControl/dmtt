using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaborControl.API.Data;
using LaborControl.API.Models;
using System.Security.Claims;

namespace LaborControl.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class EquipmentCategoriesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EquipmentCategoriesController(ApplicationDbContext context)
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

        // GET: api/equipmentcategories
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EquipmentCategoryDto>>> GetCategories()
        {
            var customerId = GetCustomerId();

            var categories = await _context.EquipmentCategories
                .Where(c => c.CustomerId == customerId)
                .Include(c => c.EquipmentTypes.Where(t => t.IsActive))
                .Where(c => c.IsActive)
                .OrderBy(c => c.DisplayOrder)
                .ThenBy(c => c.Name)
                .ToListAsync();

            var dtos = categories.Select(c => new EquipmentCategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Code = c.Code,
                Description = c.Description,
                Color = c.Color,
                Icon = c.Icon,
                DisplayOrder = c.DisplayOrder,
                IsActive = c.IsActive,
                IsPredefined = c.IsPredefined,
                TypesCount = c.EquipmentTypes.Count
            }).ToList();

            return Ok(dtos);
        }

        // GET: api/equipmentcategories/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<EquipmentCategoryDetailDto>> GetCategory(Guid id)
        {
            var customerId = GetCustomerId();

            var category = await _context.EquipmentCategories
                .Where(c => c.Id == id && c.CustomerId == customerId)
                .Include(c => c.EquipmentTypes.Where(t => t.IsActive))
                .FirstOrDefaultAsync();

            if (category == null)
            {
                return NotFound();
            }

            var dto = new EquipmentCategoryDetailDto
            {
                Id = category.Id,
                Name = category.Name,
                Code = category.Code,
                Description = category.Description,
                Color = category.Color,
                Icon = category.Icon,
                DisplayOrder = category.DisplayOrder,
                IsActive = category.IsActive,
                IsPredefined = category.IsPredefined,
                EquipmentTypes = category.EquipmentTypes.Select(t => new EquipmentTypeDto
                {
                    Id = t.Id,
                    EquipmentCategoryId = t.EquipmentCategoryId,
                    CategoryName = category.Name,
                    Name = t.Name,
                    Code = t.Code,
                    Description = t.Description,
                    Icon = t.Icon,
                    DisplayOrder = t.DisplayOrder,
                    IsActive = t.IsActive,
                    IsPredefined = t.IsPredefined
                }).OrderBy(t => t.DisplayOrder).ThenBy(t => t.Name).ToList()
            };

            return Ok(dto);
        }

        // POST: api/equipmentcategories
        [HttpPost]
        public async Task<ActionResult<EquipmentCategoryDto>> CreateCategory(CreateEquipmentCategoryRequest request)
        {
            var customerId = GetCustomerId();

            var category = new EquipmentCategory
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

            _context.EquipmentCategories.Add(category);
            await _context.SaveChangesAsync();

            var dto = new EquipmentCategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Code = category.Code,
                Description = category.Description,
                Color = category.Color,
                Icon = category.Icon,
                DisplayOrder = category.DisplayOrder,
                IsActive = category.IsActive,
                IsPredefined = category.IsPredefined,
                TypesCount = 0
            };

            return CreatedAtAction(nameof(GetCategory), new { id = category.Id }, dto);
        }

        // PUT: api/equipmentcategories/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCategory(Guid id, UpdateEquipmentCategoryRequest request)
        {
            var customerId = GetCustomerId();

            var category = await _context.EquipmentCategories
                .FirstOrDefaultAsync(c => c.Id == id && c.CustomerId == customerId);

            if (category == null)
            {
                return NotFound();
            }

            category.Name = request.Name;
            category.Code = request.Code;
            category.Description = request.Description;
            category.Color = request.Color;
            category.Icon = request.Icon;
            category.DisplayOrder = request.DisplayOrder;
            category.IsActive = request.IsActive;
            category.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/equipmentcategories/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCategory(Guid id)
        {
            var customerId = GetCustomerId();

            var category = await _context.EquipmentCategories
                .Include(c => c.EquipmentTypes)
                .FirstOrDefaultAsync(c => c.Id == id && c.CustomerId == customerId);

            if (category == null)
            {
                return NotFound();
            }

            // Vérifier si la catégorie est utilisée par des équipements
            var assetsCount = await _context.Assets
                .Include(a => a.Zone)
                    .ThenInclude(z => z.Site)
                .Where(a => a.Zone.Site.CustomerId == customerId && a.Category == category.Code)
                .CountAsync();

            if (assetsCount > 0)
            {
                return BadRequest(new { message = $"Cette catégorie est utilisée par {assetsCount} équipement(s) et ne peut pas être supprimée." });
            }

            _context.EquipmentCategories.Remove(category);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // POST: api/equipmentcategories/init-predefined
        /// <summary>
        /// Initialise les catégories et types d'équipement prédéfinis pour le client
        /// </summary>
        [HttpPost("init-predefined")]
        public async Task<ActionResult> InitPredefinedEquipmentCategories()
        {
            try
            {
                var customerId = GetCustomerId();

                // Vérifier si déjà initialisé
                var existingCount = await _context.EquipmentCategories
                    .CountAsync(c => c.CustomerId == customerId && c.IsPredefined);

                if (existingCount > 0)
                {
                    return BadRequest(new { message = $"Les catégories prédéfinies ont déjà été initialisées ({existingCount} catégories trouvées)" });
                }

                // Créer les catégories prédéfinies
                var categories = new List<EquipmentCategory>
                {
                    new EquipmentCategory { Id = Guid.NewGuid(), CustomerId = customerId, Name = "Production", Code = "PROD", Description = "Équipements de production", Color = "#3B82F6", Icon = "🏭", DisplayOrder = 1, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentCategory { Id = Guid.NewGuid(), CustomerId = customerId, Name = "Utilités", Code = "UTIL", Description = "Équipements utilitaires", Color = "#F59E0B", Icon = "⚡", DisplayOrder = 2, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentCategory { Id = Guid.NewGuid(), CustomerId = customerId, Name = "Sécurité", Code = "SECU", Description = "Équipements de sécurité", Color = "#EF4444", Icon = "🛡️", DisplayOrder = 3, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentCategory { Id = Guid.NewGuid(), CustomerId = customerId, Name = "Infrastructure", Code = "INFRA", Description = "Équipements d'infrastructure", Color = "#6366F1", Icon = "🏢", DisplayOrder = 4, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow }
                };

                _context.EquipmentCategories.AddRange(categories);
                await _context.SaveChangesAsync();

                // Créer les types prédéfinis pour chaque catégorie
                var prodCat = categories.First(c => c.Code == "PROD");
                var utilCat = categories.First(c => c.Code == "UTIL");
                var secuCat = categories.First(c => c.Code == "SECU");
                var infraCat = categories.First(c => c.Code == "INFRA");

                var types = new List<EquipmentType>
                {
                    // ========== PRODUCTION ==========
                    // Machines-outils
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = prodCat.Id, Name = "Tour", Code = "LATHE", Icon = "🔧", DisplayOrder = 1, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = prodCat.Id, Name = "Fraiseuse", Code = "MILLING_MACHINE", Icon = "⚙️", DisplayOrder = 2, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = prodCat.Id, Name = "Perceuse", Code = "DRILL", Icon = "🔩", DisplayOrder = 3, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = prodCat.Id, Name = "Presse", Code = "PRESS", Icon = "🔨", DisplayOrder = 4, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = prodCat.Id, Name = "Rectifieuse", Code = "GRINDER", Icon = "⚙️", DisplayOrder = 5, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = prodCat.Id, Name = "Centre d'usinage", Code = "MACHINING_CENTER", Icon = "🏭", DisplayOrder = 6, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },

                    // Équipements de process
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = prodCat.Id, Name = "Réacteur", Code = "REACTOR", Icon = "⚗️", DisplayOrder = 10, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = prodCat.Id, Name = "Échangeur thermique", Code = "HEAT_EXCHANGER", Icon = "🔄", DisplayOrder = 11, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = prodCat.Id, Name = "Colonne de distillation", Code = "DISTILLATION_COLUMN", Icon = "🏗️", DisplayOrder = 12, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = prodCat.Id, Name = "Séparateur", Code = "SEPARATOR", Icon = "↔️", DisplayOrder = 13, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = prodCat.Id, Name = "Mélangeur", Code = "MIXER", Icon = "🌀", DisplayOrder = 14, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = prodCat.Id, Name = "Broyeur", Code = "CRUSHER", Icon = "⚙️", DisplayOrder = 15, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = prodCat.Id, Name = "Filtre", Code = "FILTER", Icon = "🔬", DisplayOrder = 16, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },

                    // Pompes et fluides
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = prodCat.Id, Name = "Pompe centrifuge", Code = "CENTRIFUGAL_PUMP", Icon = "💧", DisplayOrder = 20, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = prodCat.Id, Name = "Pompe volumétrique", Code = "VOLUMETRIC_PUMP", Icon = "💦", DisplayOrder = 21, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = prodCat.Id, Name = "Compresseur", Code = "COMPRESSOR", Icon = "🌀", DisplayOrder = 22, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = prodCat.Id, Name = "Vanne de régulation", Code = "CONTROL_VALVE", Icon = "🔧", DisplayOrder = 23, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },

                    // Fours et thermique
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = prodCat.Id, Name = "Four industriel", Code = "INDUSTRIAL_FURNACE", Icon = "🔥", DisplayOrder = 30, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = prodCat.Id, Name = "Sécheur", Code = "DRYER", Icon = "🌡️", DisplayOrder = 31, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = prodCat.Id, Name = "Refroidisseur", Code = "COOLER", Icon = "❄️", DisplayOrder = 32, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },

                    // Transport et manutention
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = prodCat.Id, Name = "Convoyeur à bande", Code = "BELT_CONVEYOR", Icon = "➡️", DisplayOrder = 40, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = prodCat.Id, Name = "Convoyeur à rouleaux", Code = "ROLLER_CONVEYOR", Icon = "🔄", DisplayOrder = 41, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = prodCat.Id, Name = "Élévateur", Code = "ELEVATOR_CONVEYOR", Icon = "⬆️", DisplayOrder = 42, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = prodCat.Id, Name = "Pont roulant", Code = "OVERHEAD_CRANE", Icon = "🏗️", DisplayOrder = 43, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = prodCat.Id, Name = "Chariot élévateur", Code = "FORKLIFT", Icon = "🚜", DisplayOrder = 44, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },

                    // Automatisation et robotique
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = prodCat.Id, Name = "Robot industriel", Code = "INDUSTRIAL_ROBOT", Icon = "🤖", DisplayOrder = 50, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = prodCat.Id, Name = "Automate programmable", Code = "PLC", Icon = "💻", DisplayOrder = 51, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = prodCat.Id, Name = "Système de vision", Code = "VISION_SYSTEM", Icon = "👁️", DisplayOrder = 52, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },

                    // ========== UTILITÉS ==========
                    // Production d'énergie
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = utilCat.Id, Name = "Chaudière vapeur", Code = "STEAM_BOILER", Icon = "🔥", DisplayOrder = 1, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = utilCat.Id, Name = "Chaudière eau chaude", Code = "HOT_WATER_BOILER", Icon = "♨️", DisplayOrder = 2, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = utilCat.Id, Name = "Groupe électrogène", Code = "GENERATOR", Icon = "⚡", DisplayOrder = 3, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = utilCat.Id, Name = "Transformateur électrique", Code = "TRANSFORMER", Icon = "🔌", DisplayOrder = 4, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = utilCat.Id, Name = "Onduleur", Code = "UPS", Icon = "🔋", DisplayOrder = 5, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = utilCat.Id, Name = "Armoire électrique", Code = "ELECTRICAL_CABINET", Icon = "⚡", DisplayOrder = 6, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },

                    // Froid et climatisation
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = utilCat.Id, Name = "Groupe froid", Code = "CHILLER", Icon = "❄️", DisplayOrder = 10, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = utilCat.Id, Name = "Tour de refroidissement", Code = "COOLING_TOWER", Icon = "🌡️", DisplayOrder = 11, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = utilCat.Id, Name = "Centrale de traitement d'air", Code = "AHU", Icon = "🌬️", DisplayOrder = 12, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = utilCat.Id, Name = "Climatiseur", Code = "AIR_CONDITIONER", Icon = "❄️", DisplayOrder = 13, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = utilCat.Id, Name = "Ventilateur", Code = "FAN", Icon = "💨", DisplayOrder = 14, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },

                    // Air comprimé
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = utilCat.Id, Name = "Compresseur d'air", Code = "AIR_COMPRESSOR", Icon = "🌀", DisplayOrder = 20, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = utilCat.Id, Name = "Sécheur d'air", Code = "AIR_DRYER", Icon = "💨", DisplayOrder = 21, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = utilCat.Id, Name = "Réservoir d'air", Code = "AIR_RECEIVER", Icon = "🛢️", DisplayOrder = 22, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },

                    // Traitement d'eau
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = utilCat.Id, Name = "Adoucisseur", Code = "WATER_SOFTENER", Icon = "💧", DisplayOrder = 30, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = utilCat.Id, Name = "Osmoseur", Code = "REVERSE_OSMOSIS", Icon = "🔬", DisplayOrder = 31, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = utilCat.Id, Name = "Station d'épuration", Code = "WASTEWATER_TREATMENT", Icon = "♻️", DisplayOrder = 32, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = utilCat.Id, Name = "Dégraisseur", Code = "GREASE_SEPARATOR", Icon = "🛢️", DisplayOrder = 33, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = utilCat.Id, Name = "Surpresseur", Code = "BOOSTER_PUMP", Icon = "💧", DisplayOrder = 34, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },

                    // ========== SÉCURITÉ ==========
                    // Protection incendie
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = secuCat.Id, Name = "Détecteur de fumée", Code = "SMOKE_DETECTOR", Icon = "🚨", DisplayOrder = 1, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = secuCat.Id, Name = "Détecteur de chaleur", Code = "HEAT_DETECTOR", Icon = "🔥", DisplayOrder = 2, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = secuCat.Id, Name = "Extincteur", Code = "EXTINGUISHER", Icon = "🧯", DisplayOrder = 3, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = secuCat.Id, Name = "RIA (Robinet Incendie Armé)", Code = "FIRE_HOSE_REEL", Icon = "🚒", DisplayOrder = 4, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = secuCat.Id, Name = "Sprinkler", Code = "SPRINKLER", Icon = "💦", DisplayOrder = 5, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = secuCat.Id, Name = "Centrale incendie", Code = "FIRE_ALARM_PANEL", Icon = "🔴", DisplayOrder = 6, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = secuCat.Id, Name = "Déclencheur manuel", Code = "MANUAL_CALL_POINT", Icon = "🔴", DisplayOrder = 7, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },

                    // Contrôle d'accès et surveillance
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = secuCat.Id, Name = "Caméra de surveillance", Code = "CCTV_CAMERA", Icon = "📹", DisplayOrder = 10, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = secuCat.Id, Name = "Lecteur de badge", Code = "CARD_READER", Icon = "💳", DisplayOrder = 11, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = secuCat.Id, Name = "Portique de sécurité", Code = "SECURITY_GATE", Icon = "🚧", DisplayOrder = 12, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = secuCat.Id, Name = "Tourniquet", Code = "TURNSTILE", Icon = "🚪", DisplayOrder = 13, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = secuCat.Id, Name = "Interphone", Code = "INTERCOM", Icon = "📞", DisplayOrder = 14, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },

                    // Alarmes et détection
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = secuCat.Id, Name = "Alarme intrusion", Code = "INTRUSION_ALARM", Icon = "🔔", DisplayOrder = 20, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = secuCat.Id, Name = "Détecteur de mouvement", Code = "MOTION_DETECTOR", Icon = "👁️", DisplayOrder = 21, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = secuCat.Id, Name = "Détecteur de gaz", Code = "GAS_DETECTOR", Icon = "⚠️", DisplayOrder = 22, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = secuCat.Id, Name = "Sirène d'alarme", Code = "ALARM_SIREN", Icon = "🔊", DisplayOrder = 23, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },

                    // Éclairage de sécurité
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = secuCat.Id, Name = "BAES (Bloc Autonome)", Code = "EMERGENCY_LIGHT", Icon = "💡", DisplayOrder = 30, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = secuCat.Id, Name = "Éclairage de sécurité", Code = "SAFETY_LIGHTING", Icon = "🔦", DisplayOrder = 31, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },

                    // Équipements de protection
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = secuCat.Id, Name = "Douche de sécurité", Code = "SAFETY_SHOWER", Icon = "🚿", DisplayOrder = 40, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = secuCat.Id, Name = "Lave-œil", Code = "EYE_WASH", Icon = "👁️", DisplayOrder = 41, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = secuCat.Id, Name = "Défibrillateur", Code = "DEFIBRILLATOR", Icon = "❤️", DisplayOrder = 42, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = secuCat.Id, Name = "Armoire à pharmacie", Code = "FIRST_AID_KIT", Icon = "🏥", DisplayOrder = 43, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },

                    // ========== INFRASTRUCTURE ==========
                    // Bâtiments et locaux
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = infraCat.Id, Name = "Bâtiment", Code = "BUILDING", Icon = "🏢", DisplayOrder = 1, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = infraCat.Id, Name = "Local technique", Code = "TECH_ROOM", Icon = "🔧", DisplayOrder = 2, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = infraCat.Id, Name = "Atelier", Code = "WORKSHOP", Icon = "🏭", DisplayOrder = 3, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = infraCat.Id, Name = "Entrepôt", Code = "WAREHOUSE", Icon = "📦", DisplayOrder = 4, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = infraCat.Id, Name = "Bureau", Code = "OFFICE", Icon = "🏢", DisplayOrder = 5, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },

                    // Circulation verticale
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = infraCat.Id, Name = "Ascenseur", Code = "ELEVATOR", Icon = "🛗", DisplayOrder = 10, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = infraCat.Id, Name = "Monte-charge", Code = "GOODS_LIFT", Icon = "📦", DisplayOrder = 11, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = infraCat.Id, Name = "Escalier mécanique", Code = "ESCALATOR", Icon = "🚶", DisplayOrder = 12, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = infraCat.Id, Name = "Tapis roulant", Code = "MOVING_WALKWAY", Icon = "➡️", DisplayOrder = 13, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },

                    // Portes et accès
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = infraCat.Id, Name = "Porte automatique", Code = "AUTO_DOOR", Icon = "🚪", DisplayOrder = 20, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = infraCat.Id, Name = "Porte sectionnelle", Code = "SECTIONAL_DOOR", Icon = "🚪", DisplayOrder = 21, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = infraCat.Id, Name = "Rideau métallique", Code = "ROLLER_SHUTTER", Icon = "🪟", DisplayOrder = 22, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = infraCat.Id, Name = "Portail", Code = "GATE", Icon = "🚧", DisplayOrder = 23, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = infraCat.Id, Name = "Barrière levante", Code = "BARRIER_GATE", Icon = "🚧", DisplayOrder = 24, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },

                    // Quais et logistique
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = infraCat.Id, Name = "Quai de chargement", Code = "LOADING_DOCK", Icon = "🚚", DisplayOrder = 30, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = infraCat.Id, Name = "Niveleur de quai", Code = "DOCK_LEVELER", Icon = "⬍", DisplayOrder = 31, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = infraCat.Id, Name = "Sas d'étanchéité", Code = "DOCK_SHELTER", Icon = "🚪", DisplayOrder = 32, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },

                    // Toiture et enveloppe
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = infraCat.Id, Name = "Toiture", Code = "ROOF", Icon = "🏠", DisplayOrder = 40, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = infraCat.Id, Name = "Lanterneau", Code = "SKYLIGHT", Icon = "🪟", DisplayOrder = 41, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = infraCat.Id, Name = "Exutoire de fumée", Code = "SMOKE_VENT", Icon = "💨", DisplayOrder = 42, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },

                    // Réseaux et distribution
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = infraCat.Id, Name = "Réseau électrique", Code = "ELECTRICAL_NETWORK", Icon = "⚡", DisplayOrder = 50, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = infraCat.Id, Name = "Réseau eau", Code = "WATER_NETWORK", Icon = "💧", DisplayOrder = 51, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = infraCat.Id, Name = "Réseau gaz", Code = "GAS_NETWORK", Icon = "🔥", DisplayOrder = 52, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = infraCat.Id, Name = "Réseau air comprimé", Code = "COMPRESSED_AIR_NETWORK", Icon = "🌀", DisplayOrder = 53, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = infraCat.Id, Name = "Réseau assainissement", Code = "SEWAGE_NETWORK", Icon = "♻️", DisplayOrder = 54, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },

                    // Parking et voirie
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = infraCat.Id, Name = "Parking", Code = "PARKING", Icon = "🅿️", DisplayOrder = 60, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = infraCat.Id, Name = "Borne de recharge", Code = "CHARGING_STATION", Icon = "🔌", DisplayOrder = 61, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = infraCat.Id, Name = "Éclairage extérieur", Code = "OUTDOOR_LIGHTING", Icon = "💡", DisplayOrder = 62, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow },
                    new EquipmentType { Id = Guid.NewGuid(), CustomerId = customerId, EquipmentCategoryId = infraCat.Id, Name = "Voirie", Code = "ROAD", Icon = "🛣️", DisplayOrder = 63, IsPredefined = true, IsActive = true, CreatedAt = DateTime.UtcNow }
                };

                _context.EquipmentTypes.AddRange(types);
                await _context.SaveChangesAsync();

                return Ok(new { message = $"Initialisation réussie : {categories.Count} catégories et {types.Count} types créés", categoriesCount = categories.Count, typesCount = types.Count });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Erreur lors de l'initialisation : {ex.Message}" });
            }
        }
    }

    // DTOs
    public class EquipmentCategoryDto
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
        public int TypesCount { get; set; }
    }

    public class EquipmentCategoryDetailDto
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
        public List<EquipmentTypeDto> EquipmentTypes { get; set; } = new();
    }

    public class CreateEquipmentCategoryRequest
    {
        public string Name { get; set; } = "";
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; } = 0;
    }

    public class UpdateEquipmentCategoryRequest
    {
        public string Name { get; set; } = "";
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string? Color { get; set; }
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class EquipmentTypeDto
    {
        public Guid Id { get; set; }
        public Guid EquipmentCategoryId { get; set; }
        public string CategoryName { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public bool IsPredefined { get; set; }
    }
}
