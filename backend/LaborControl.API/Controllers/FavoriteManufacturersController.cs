using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaborControl.API.Data;
using LaborControl.API.Models;

namespace LaborControl.API.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class FavoriteManufacturersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public FavoriteManufacturersController(ApplicationDbContext context)
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

        // GET: api/favoritemanufacturers
        [HttpGet]
        public async Task<ActionResult<IEnumerable<FavoriteManufacturerDto>>> GetManufacturers()
        {
            var customerId = GetCustomerId();

            var manufacturers = await _context.FavoriteManufacturers
                .Where(m => m.CustomerId == customerId && m.IsActive)
                .OrderBy(m => m.DisplayOrder)
                .ThenBy(m => m.Name)
                .ToListAsync();

            var dtos = manufacturers.Select(m => new FavoriteManufacturerDto
            {
                Id = m.Id,
                Name = m.Name,
                Country = m.Country,
                Website = m.Website,
                ContactEmail = m.ContactEmail,
                ContactPhone = m.ContactPhone,
                DisplayOrder = m.DisplayOrder,
                IsActive = m.IsActive
            }).ToList();

            return Ok(dtos);
        }

        // GET: api/favoritemanufacturers/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<FavoriteManufacturerDto>> GetManufacturer(Guid id)
        {
            var customerId = GetCustomerId();

            var manufacturer = await _context.FavoriteManufacturers
                .FirstOrDefaultAsync(m => m.Id == id && m.CustomerId == customerId);

            if (manufacturer == null)
            {
                return NotFound();
            }

            var dto = new FavoriteManufacturerDto
            {
                Id = manufacturer.Id,
                Name = manufacturer.Name,
                Country = manufacturer.Country,
                Website = manufacturer.Website,
                ContactEmail = manufacturer.ContactEmail,
                ContactPhone = manufacturer.ContactPhone,
                DisplayOrder = manufacturer.DisplayOrder,
                IsActive = manufacturer.IsActive
            };

            return Ok(dto);
        }

        // POST: api/favoritemanufacturers
        [HttpPost]
        public async Task<ActionResult<FavoriteManufacturerDto>> CreateManufacturer(CreateFavoriteManufacturerRequest request)
        {
            var customerId = GetCustomerId();

            var manufacturer = new FavoriteManufacturer
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                Name = request.Name,
                Country = request.Country,
                Website = request.Website,
                ContactEmail = request.ContactEmail,
                ContactPhone = request.ContactPhone,
                DisplayOrder = request.DisplayOrder
            };

            _context.FavoriteManufacturers.Add(manufacturer);
            await _context.SaveChangesAsync();

            var dto = new FavoriteManufacturerDto
            {
                Id = manufacturer.Id,
                Name = manufacturer.Name,
                Country = manufacturer.Country,
                Website = manufacturer.Website,
                ContactEmail = manufacturer.ContactEmail,
                ContactPhone = manufacturer.ContactPhone,
                DisplayOrder = manufacturer.DisplayOrder,
                IsActive = manufacturer.IsActive
            };

            return CreatedAtAction(nameof(GetManufacturer), new { id = manufacturer.Id }, dto);
        }

        // PUT: api/favoritemanufacturers/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateManufacturer(Guid id, UpdateFavoriteManufacturerRequest request)
        {
            var customerId = GetCustomerId();

            var manufacturer = await _context.FavoriteManufacturers
                .FirstOrDefaultAsync(m => m.Id == id && m.CustomerId == customerId);

            if (manufacturer == null)
            {
                return NotFound();
            }

            manufacturer.Name = request.Name;
            manufacturer.Country = request.Country;
            manufacturer.Website = request.Website;
            manufacturer.ContactEmail = request.ContactEmail;
            manufacturer.ContactPhone = request.ContactPhone;
            manufacturer.DisplayOrder = request.DisplayOrder;
            manufacturer.IsActive = request.IsActive;
            manufacturer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/favoritemanufacturers/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteManufacturer(Guid id)
        {
            var customerId = GetCustomerId();

            var manufacturer = await _context.FavoriteManufacturers
                .FirstOrDefaultAsync(m => m.Id == id && m.CustomerId == customerId);

            if (manufacturer == null)
            {
                return NotFound();
            }

            _context.FavoriteManufacturers.Remove(manufacturer);
            await _context.SaveChangesAsync();

            return NoContent();
        }
    }

    // DTOs
    public class FavoriteManufacturerDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public string? Country { get; set; }
        public string? Website { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateFavoriteManufacturerRequest
    {
        public string Name { get; set; } = "";
        public string? Country { get; set; }
        public string? Website { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public int DisplayOrder { get; set; } = 0;
    }

    public class UpdateFavoriteManufacturerRequest
    {
        public string Name { get; set; } = "";
        public string? Country { get; set; }
        public string? Website { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }
}
