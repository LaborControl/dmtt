using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaborControl.API.Data;
using LaborControl.API.DTOs;
using LaborControl.API.Models;

namespace LaborControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class SitesController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<SitesController> _logger;

        public SitesController(ApplicationDbContext context, ILogger<SitesController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Récupère tous les sites du client connecté
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<SiteResponse>>> GetSites()
        {
            var customerIdClaim = User.FindFirst("CustomerId");
            if (customerIdClaim == null || !Guid.TryParse(customerIdClaim.Value, out var customerId))
            {
                return BadRequest(new { message = "CustomerId introuvable dans le token" });
            }

            // Optimisation: AsNoTracking et pas besoin d'Include avant Select
            var sites = await _context.Sites
                .AsNoTracking()
                .Where(s => s.CustomerId == customerId)
                .OrderBy(s => s.Name)
                .Select(s => new SiteResponse
                {
                    Id = s.Id,
                    CustomerId = s.CustomerId,
                    Name = s.Name,
                    Code = s.Code,
                    Address = s.Address,
                    City = s.City,
                    PostalCode = s.PostalCode,
                    Country = s.Country,
                    ContactName = s.ContactName,
                    ContactPhone = s.ContactPhone,
                    ContactEmail = s.ContactEmail,
                    Siret = s.Siret,
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                    ZonesCount = s.Zones.Count
                })
                .ToListAsync();

            _logger.LogInformation($"[GET SITES] Chargement de {sites.Count} site(s) pour CustomerId: {customerId}");
            if (sites.Count > 0)
            {
                _logger.LogInformation($"[GET SITES] Sites: {string.Join(", ", sites.Select(s => $"{s.Name} (Id: {s.Id})"))}");
            }

            return Ok(sites);
        }

        /// <summary>
        /// Récupère un site par son ID avec ses zones et équipes
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<SiteDetailResponse>> GetSite(Guid id)
        {
            var customerIdClaim = User.FindFirst("CustomerId");
            if (customerIdClaim == null || !Guid.TryParse(customerIdClaim.Value, out var customerId))
            {
                return BadRequest(new { message = "CustomerId introuvable dans le token" });
            }

            var site = await _context.Sites
                .Include(s => s.Zones)
                .Where(s => s.Id == id && s.CustomerId == customerId)
                .Select(s => new SiteDetailResponse
                {
                    Id = s.Id,
                    CustomerId = s.CustomerId,
                    Name = s.Name,
                    Code = s.Code,
                    Address = s.Address,
                    City = s.City,
                    PostalCode = s.PostalCode,
                    Country = s.Country,
                    Latitude = s.Latitude,
                    Longitude = s.Longitude,
                    ContactName = s.ContactName,
                    ContactPhone = s.ContactPhone,
                    ContactEmail = s.ContactEmail,
                    Siret = s.Siret,
                    IsActive = s.IsActive,
                    CreatedAt = s.CreatedAt,
                    UpdatedAt = s.UpdatedAt,
                    Zones = s.Zones.Select(z => new ZoneInfo
                    {
                        Id = z.Id,
                        Name = z.Name,
                        Code = z.Code,
                        Type = z.Type,
                        ParentZoneId = z.ParentZoneId,
                        Level = z.Level,
                        IsActive = z.IsActive
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (site == null)
            {
                return NotFound(new { message = "Site non trouvé" });
            }

            return Ok(site);
        }

        /// <summary>
        /// Crée un nouveau site
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<SiteResponse>> CreateSite([FromBody] CreateSiteRequest request)
        {
            var customerIdClaim = User.FindFirst("CustomerId");
            if (customerIdClaim == null || !Guid.TryParse(customerIdClaim.Value, out var customerId))
            {
                return BadRequest(new { message = "CustomerId introuvable dans le token" });
            }

            var site = new Site
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                Name = request.Name,
                Code = request.Code,
                Address = request.Address,
                City = request.City,
                PostalCode = request.PostalCode,
                Country = request.Country ?? "France",
                Latitude = request.Latitude,
                Longitude = request.Longitude,
                ContactName = request.ContactName,
                ContactPhone = request.ContactPhone,
                ContactEmail = request.ContactEmail,
                Siret = request.Siret,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Sites.Add(site);
            await _context.SaveChangesAsync();

            var siteResponse = new SiteResponse
            {
                Id = site.Id,
                CustomerId = site.CustomerId,
                Name = site.Name,
                Code = site.Code,
                Address = site.Address,
                City = site.City,
                PostalCode = site.PostalCode,
                Country = site.Country,
                ContactName = site.ContactName,
                ContactPhone = site.ContactPhone,
                ContactEmail = site.ContactEmail,
                Siret = site.Siret,
                IsActive = site.IsActive,
                CreatedAt = site.CreatedAt,
                ZonesCount = 0
            };

            return CreatedAtAction(nameof(GetSite), new { id = site.Id }, siteResponse);
        }

        /// <summary>
        /// Met à jour un site existant
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<SiteResponse>> UpdateSite(Guid id, [FromBody] UpdateSiteRequest request)
        {
            var customerIdClaim = User.FindFirst("CustomerId");
            if (customerIdClaim == null || !Guid.TryParse(customerIdClaim.Value, out var customerId))
            {
                return BadRequest(new { message = "CustomerId introuvable dans le token" });
            }

            var site = await _context.Sites
                .FirstOrDefaultAsync(s => s.Id == id && s.CustomerId == customerId);

            if (site == null)
            {
                return NotFound(new { message = "Site non trouvé" });
            }

            site.Name = request.Name;
            site.Code = request.Code;
            site.Address = request.Address;
            site.City = request.City;
            site.PostalCode = request.PostalCode;
            site.Country = request.Country ?? "France";
            site.Latitude = request.Latitude;
            site.Longitude = request.Longitude;
            site.ContactName = request.ContactName;
            site.ContactPhone = request.ContactPhone;
            site.ContactEmail = request.ContactEmail;
            site.Siret = request.Siret;
            site.IsActive = request.IsActive;
            site.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var siteResponse = new SiteResponse
            {
                Id = site.Id,
                CustomerId = site.CustomerId,
                Name = site.Name,
                Code = site.Code,
                Address = site.Address,
                City = site.City,
                PostalCode = site.PostalCode,
                Country = site.Country,
                ContactName = site.ContactName,
                ContactPhone = site.ContactPhone,
                ContactEmail = site.ContactEmail,
                Siret = site.Siret,
                IsActive = site.IsActive,
                CreatedAt = site.CreatedAt,
                UpdatedAt = site.UpdatedAt,
                ZonesCount = await _context.Zones.CountAsync(z => z.SiteId == id)
            };

            return Ok(siteResponse);
        }

        /// <summary>
        /// Supprime un site
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteSite(Guid id)
        {
            var customerIdClaim = User.FindFirst("CustomerId");
            if (customerIdClaim == null || !Guid.TryParse(customerIdClaim.Value, out var customerId))
            {
                return BadRequest(new { message = "CustomerId introuvable dans le token" });
            }

            var site = await _context.Sites
                .Include(s => s.Zones)
                .FirstOrDefaultAsync(s => s.Id == id && s.CustomerId == customerId);

            if (site == null)
            {
                return NotFound(new { message = "Site non trouvé" });
            }

            // Vérifier s'il y a des zones actives
            if (site.Zones.Any(z => z.IsActive))
            {
                return BadRequest(new { message = "Impossible de supprimer un site contenant des zones actives" });
            }

            _context.Sites.Remove(site);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Site supprimé avec succès" });
        }

        /// <summary>
        /// Récupère toutes les zones d'un site
        /// </summary>
        [HttpGet("{id}/zones")]
        public async Task<ActionResult<List<ZoneInfo>>> GetSiteZones(Guid id)
        {
            var customerIdClaim = User.FindFirst("CustomerId");
            if (customerIdClaim == null || !Guid.TryParse(customerIdClaim.Value, out var customerId))
            {
                return BadRequest(new { message = "CustomerId introuvable dans le token" });
            }

            // Vérifier que le site appartient au client
            var siteExists = await _context.Sites
                .AnyAsync(s => s.Id == id && s.CustomerId == customerId);

            if (!siteExists)
            {
                return NotFound(new { message = "Site non trouvé" });
            }

            var zones = await _context.Zones
                .Where(z => z.SiteId == id && z.IsActive)
                .OrderBy(z => z.Level)
                .ThenBy(z => z.Name)
                .Select(z => new ZoneInfo
                {
                    Id = z.Id,
                    Name = z.Name,
                    Code = z.Code,
                    Type = z.Type,
                    ParentZoneId = z.ParentZoneId,
                    Level = z.Level,
                    IsActive = z.IsActive
                })
                .ToListAsync();

            return Ok(zones);
        }

        /// <summary>
        /// Récupère le nombre total de sites de tous les clients
        /// </summary>
        [HttpGet("count/all")]
        [AllowAnonymous]
        public async Task<ActionResult<int>> GetAllSitesCount()
        {
            var count = await _context.Sites
                .Where(s => s.IsActive)
                .CountAsync();

            return Ok(count);
        }

        /// <summary>
        /// Récupère les tendances du nombre de sites (pour dashboard)
        /// </summary>
        [HttpGet("count/trend")]
        [AllowAnonymous]
        public async Task<ActionResult<TrendResponse>> GetSitesCountTrend()
        {
            var now = DateTime.UtcNow;
            var firstDayThisMonth = new DateTime(now.Year, now.Month, 1);
            var firstDayLastMonth = firstDayThisMonth.AddMonths(-1);

            var total = await _context.Sites
                .Where(s => s.IsActive)
                .CountAsync();

            var thisMonth = await _context.Sites
                .Where(s => s.IsActive && s.CreatedAt >= firstDayThisMonth)
                .CountAsync();

            var lastMonth = await _context.Sites
                .Where(s => s.IsActive && s.CreatedAt >= firstDayLastMonth && s.CreatedAt < firstDayThisMonth)
                .CountAsync();

            var percentChange = lastMonth > 0
                ? ((thisMonth - lastMonth) / (double)lastMonth) * 100
                : 0;

            return Ok(new TrendResponse
            {
                Total = total,
                ThisMonth = thisMonth,
                LastMonth = lastMonth,
                PercentChange = Math.Round(percentChange, 1)
            });
        }
    }
}
