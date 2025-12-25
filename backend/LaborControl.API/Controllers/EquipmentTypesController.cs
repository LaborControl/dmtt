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
    public class EquipmentTypesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public EquipmentTypesController(ApplicationDbContext context)
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

        // GET: api/equipmenttypes
        [HttpGet]
        public async Task<ActionResult<IEnumerable<EquipmentTypeDto>>> GetTypes()
        {
            var customerId = GetCustomerId();

            var types = await _context.EquipmentTypes
                .Where(t => t.CustomerId == customerId && t.IsActive)
                .Include(t => t.EquipmentCategory)
                .OrderBy(t => t.EquipmentCategory.DisplayOrder)
                .ThenBy(t => t.DisplayOrder)
                .ThenBy(t => t.Name)
                .ToListAsync();

            var dtos = types.Select(t => new EquipmentTypeDto
            {
                Id = t.Id,
                EquipmentCategoryId = t.EquipmentCategoryId,
                CategoryName = t.EquipmentCategory?.Name ?? "",
                Name = t.Name,
                Code = t.Code,
                Description = t.Description,
                Icon = t.Icon,
                DisplayOrder = t.DisplayOrder,
                IsActive = t.IsActive,
                IsPredefined = t.IsPredefined
            }).ToList();

            return Ok(dtos);
        }

        // GET: api/equipmenttypes/category/{categoryId}
        [HttpGet("category/{categoryId}")]
        public async Task<ActionResult<IEnumerable<EquipmentTypeDto>>> GetTypesByCategory(Guid categoryId)
        {
            var customerId = GetCustomerId();

            var types = await _context.EquipmentTypes
                .Where(t => t.EquipmentCategoryId == categoryId && t.CustomerId == customerId && t.IsActive)
                .Include(t => t.EquipmentCategory)
                .OrderBy(t => t.DisplayOrder)
                .ThenBy(t => t.Name)
                .ToListAsync();

            var dtos = types.Select(t => new EquipmentTypeDto
            {
                Id = t.Id,
                EquipmentCategoryId = t.EquipmentCategoryId,
                CategoryName = t.EquipmentCategory?.Name ?? "",
                Name = t.Name,
                Code = t.Code,
                Description = t.Description,
                Icon = t.Icon,
                DisplayOrder = t.DisplayOrder,
                IsActive = t.IsActive,
                IsPredefined = t.IsPredefined
            }).ToList();

            return Ok(dtos);
        }

        // GET: api/equipmenttypes/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<EquipmentTypeDto>> GetType(Guid id)
        {
            var customerId = GetCustomerId();

            var type = await _context.EquipmentTypes
                .Where(t => t.Id == id && t.CustomerId == customerId)
                .Include(t => t.EquipmentCategory)
                .FirstOrDefaultAsync();

            if (type == null)
            {
                return NotFound();
            }

            var dto = new EquipmentTypeDto
            {
                Id = type.Id,
                EquipmentCategoryId = type.EquipmentCategoryId,
                CategoryName = type.EquipmentCategory?.Name ?? "",
                Name = type.Name,
                Code = type.Code,
                Description = type.Description,
                Icon = type.Icon,
                DisplayOrder = type.DisplayOrder,
                IsActive = type.IsActive,
                IsPredefined = type.IsPredefined
            };

            return Ok(dto);
        }

        // POST: api/equipmenttypes
        [HttpPost]
        public async Task<ActionResult<EquipmentTypeDto>> CreateType(CreateEquipmentTypeRequest request)
        {
            var customerId = GetCustomerId();

            // Vérifier que la catégorie existe et appartient au client
            var categoryExists = await _context.EquipmentCategories
                .AnyAsync(c => c.Id == request.EquipmentCategoryId && c.CustomerId == customerId);

            if (!categoryExists)
            {
                return BadRequest(new { message = "La catégorie spécifiée n'existe pas." });
            }

            var type = new EquipmentType
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                EquipmentCategoryId = request.EquipmentCategoryId,
                Name = request.Name,
                Code = request.Code,
                Description = request.Description,
                Icon = request.Icon,
                DisplayOrder = request.DisplayOrder,
                IsPredefined = false
            };

            _context.EquipmentTypes.Add(type);
            await _context.SaveChangesAsync();

            // Recharger avec la catégorie
            type = await _context.EquipmentTypes
                .Include(t => t.EquipmentCategory)
                .FirstAsync(t => t.Id == type.Id);

            var dto = new EquipmentTypeDto
            {
                Id = type.Id,
                EquipmentCategoryId = type.EquipmentCategoryId,
                CategoryName = type.EquipmentCategory?.Name ?? "",
                Name = type.Name,
                Code = type.Code,
                Description = type.Description,
                Icon = type.Icon,
                DisplayOrder = type.DisplayOrder,
                IsActive = type.IsActive,
                IsPredefined = type.IsPredefined
            };

            return CreatedAtAction(nameof(GetType), new { id = type.Id }, dto);
        }

        // PUT: api/equipmenttypes/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateType(Guid id, UpdateEquipmentTypeRequest request)
        {
            var customerId = GetCustomerId();

            var type = await _context.EquipmentTypes
                .FirstOrDefaultAsync(t => t.Id == id && t.CustomerId == customerId);

            if (type == null)
            {
                return NotFound();
            }

            // Vérifier que la catégorie existe et appartient au client
            var categoryExists = await _context.EquipmentCategories
                .AnyAsync(c => c.Id == request.EquipmentCategoryId && c.CustomerId == customerId);

            if (!categoryExists)
            {
                return BadRequest(new { message = "La catégorie spécifiée n'existe pas." });
            }

            type.EquipmentCategoryId = request.EquipmentCategoryId;
            type.Name = request.Name;
            type.Code = request.Code;
            type.Description = request.Description;
            type.Icon = request.Icon;
            type.DisplayOrder = request.DisplayOrder;
            type.IsActive = request.IsActive;
            type.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/equipmenttypes/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteType(Guid id)
        {
            var customerId = GetCustomerId();

            var type = await _context.EquipmentTypes
                .FirstOrDefaultAsync(t => t.Id == id && t.CustomerId == customerId);

            if (type == null)
            {
                return NotFound();
            }

            // Vérifier si le type est utilisé par des équipements
            var assetsCount = await _context.Assets
                .Include(a => a.Zone)
                    .ThenInclude(z => z.Site)
                .Where(a => a.Zone.Site.CustomerId == customerId && a.Type == type.Code)
                .CountAsync();

            if (assetsCount > 0)
            {
                return BadRequest(new { message = $"Ce type est utilisé par {assetsCount} équipement(s) et ne peut pas être supprimé." });
            }

            _context.EquipmentTypes.Remove(type);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    // DTOs
    public class CreateEquipmentTypeRequest
    {
        public Guid EquipmentCategoryId { get; set; }
        public string Name { get; set; } = "";
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; } = 0;
    }

    public class UpdateEquipmentTypeRequest
    {
        public Guid EquipmentCategoryId { get; set; }
        public string Name { get; set; } = "";
        public string? Code { get; set; }
        public string? Description { get; set; }
        public string? Icon { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }
}
