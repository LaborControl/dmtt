using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaborControl.API.Data;
using LaborControl.API.Models;
using LaborControl.API.DTOs;
using System.Security.Claims;

namespace LaborControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CustomersController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public CustomersController(ApplicationDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Récupère les informations du Customer de l'utilisateur connecté
        /// </summary>
        [HttpGet("current")]
        [HttpGet("me")]
        public async Task<ActionResult<CustomerResponse>> GetCurrentCustomer()
        {
            // Récupérer le CustomerId du token JWT
            var customerIdClaim = User.FindFirst("CustomerId");
            if (customerIdClaim == null || !Guid.TryParse(customerIdClaim.Value, out var customerId) || customerId == Guid.Empty)
            {
                // Retourner une réponse vide pour les utilisateurs sans customer (SUPERADMIN, etc.)
                return Ok(new CustomerResponse
                {
                    Id = Guid.Empty,
                    Name = "N/A",
                    Sector = "N/A",
                    SubscriptionPlan = "N/A",
                    ContactName = null,
                    ContactEmail = null,
                    ContactPhone = null,
                    Address = null,
                    Siret = null,
                    Description = null,
                    CreatedAt = DateTime.UtcNow
                });
            }

            var customer = await _context.Customers
                .Where(c => c.Id == customerId && c.IsActive)
                .Select(c => new CustomerResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    // TODO: Sector est maintenant au niveau des Teams, pas du Customer
                    Sector = "GENERAL", // Valeur par défaut temporaire
                    SubscriptionPlan = c.SubscriptionPlan,
                    ContactName = c.ContactName,
                    ContactEmail = c.ContactEmail,
                    ContactPhone = c.ContactPhone,
                    Address = c.Address,
                    Siret = c.Siret,
                    Description = c.Description,
                    CreatedAt = c.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (customer == null)
            {
                return NotFound(new { message = "Client non trouvé" });
            }

            return Ok(customer);
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<CustomerResponse>>> GetCustomers()
        {
            var customers = await _context.Customers
                .Where(c => c.IsActive)
                .OrderByDescending(c => c.CreatedAt)
                .Select(c => new CustomerResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    // TODO: Sector est maintenant au niveau des Teams, pas du Customer
                    Sector = "GENERAL", // Valeur par défaut temporaire
                    SubscriptionPlan = c.SubscriptionPlan,
                    ContactName = c.ContactName,
                    ContactEmail = c.ContactEmail,
                    ContactPhone = c.ContactPhone,
                    Address = c.Address,
                    Siret = c.Siret,
                    Description = c.Description,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return Ok(customers);
        }

        [HttpGet("recent")]
        public async Task<ActionResult<IEnumerable<CustomerResponse>>> GetRecentCustomers([FromQuery] int limit = 10)
        {
            var customers = await _context.Customers
                .Where(c => c.IsActive)
                .OrderByDescending(c => c.CreatedAt)
                .Take(limit)
                .Select(c => new CustomerResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    // TODO: Sector est maintenant au niveau des Teams, pas du Customer
                    Sector = "GENERAL", // Valeur par défaut temporaire
                    SubscriptionPlan = c.SubscriptionPlan,
                    ContactName = c.ContactName,
                    ContactEmail = c.ContactEmail,
                    ContactPhone = c.ContactPhone,
                    Address = c.Address,
                    Siret = c.Siret,
                    Description = c.Description,
                    CreatedAt = c.CreatedAt
                })
                .ToListAsync();

            return Ok(customers);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CustomerResponse>> GetCustomer(Guid id)
        {
            var customer = await _context.Customers
                .Where(c => c.Id == id && c.IsActive)
                .Select(c => new CustomerResponse
                {
                    Id = c.Id,
                    Name = c.Name,
                    // TODO: Sector est maintenant au niveau des Teams, pas du Customer
                    Sector = "GENERAL", // Valeur par défaut temporaire
                    SubscriptionPlan = c.SubscriptionPlan,
                    ContactName = c.ContactName,
                    ContactEmail = c.ContactEmail,
                    ContactPhone = c.ContactPhone,
                    Address = c.Address,
                    Siret = c.Siret,
                    Description = c.Description,
                    CreatedAt = c.CreatedAt
                })
                .FirstOrDefaultAsync();

            if (customer == null)
            {
                return NotFound(new { message = "Client non trouvé" });
            }

            return Ok(customer);
        }

        [HttpPost]
        public async Task<ActionResult<CustomerResponse>> CreateCustomer(CreateCustomerRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
            {
                return BadRequest(new { message = "Le nom du client est obligatoire" });
            }

            var customer = new Customer
            {
                Id = Guid.NewGuid(),
                Name = request.Name.Trim(),
                // TODO: Sector supprimé du modèle Customer - maintenant au niveau des Teams
                // Sector = request.Sector ?? "GENERAL",
                SubscriptionPlan = request.SubscriptionPlan ?? "free",
                ContactName = request.ContactName,
                ContactEmail = request.ContactEmail,
                ContactPhone = request.ContactPhone,
                Address = request.Address,
                Siret = request.Siret,
                Description = request.Description,
                IsMultiSite = false,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Customers.Add(customer);
            await _context.SaveChangesAsync();

            var response = new CustomerResponse
            {
                Id = customer.Id,
                Name = customer.Name,
                // TODO: Sector est maintenant au niveau des Teams, pas du Customer
                Sector = request.Sector ?? "GENERAL", // On retourne la valeur du request pour compatibilité
                SubscriptionPlan = customer.SubscriptionPlan,
                ContactName = customer.ContactName,
                ContactEmail = customer.ContactEmail,
                ContactPhone = customer.ContactPhone,
                Address = customer.Address,
                Siret = customer.Siret,
                Description = customer.Description,
                CreatedAt = customer.CreatedAt
            };

            return CreatedAtAction(nameof(GetCustomer), new { id = customer.Id }, response);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateCustomer(Guid id, CreateCustomerRequest request)
        {
            var customer = await _context.Customers.FindAsync(id);

            if (customer == null || !customer.IsActive)
            {
                return NotFound(new { message = "Client non trouvé" });
            }

            if (!string.IsNullOrWhiteSpace(request.Name))
            {
                customer.Name = request.Name.Trim();
            }

            // TODO: Sector supprimé du modèle Customer - maintenant au niveau des Teams
            // if (!string.IsNullOrWhiteSpace(request.Sector))
            // {
            //     customer.Sector = request.Sector;
            // }

            if (!string.IsNullOrWhiteSpace(request.SubscriptionPlan))
            {
                customer.SubscriptionPlan = request.SubscriptionPlan;
            }

            customer.ContactName = request.ContactName;
            customer.ContactEmail = request.ContactEmail;
            customer.ContactPhone = request.ContactPhone;
            customer.Address = request.Address;
            customer.Siret = request.Siret;
            customer.Description = request.Description;
            customer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCustomer(Guid id)
        {
            var customer = await _context.Customers.FindAsync(id);

            if (customer == null)
            {
                return NotFound(new { message = "Client non trouvé" });
            }

            customer.IsActive = false;
            customer.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// Récupère le nombre total de clients actifs
        /// </summary>
        [HttpGet("count/total")]
        [AllowAnonymous]
        public async Task<ActionResult<int>> GetTotalCustomersCount()
        {
            var count = await _context.Customers
                .Where(c => c.IsActive)
                .CountAsync();

            return Ok(count);
        }

        /// <summary>
        /// Récupère les abonnements récents (préparé pour Stripe)
        /// </summary>
        [HttpGet("recent-subscriptions")]
        [AllowAnonymous]
        public async Task<ActionResult<List<SubscriptionDTO>>> GetRecentSubscriptions([FromQuery] int limit = 10)
        {
            var subscriptions = await _context.Customers
                .Where(c => c.IsActive && !string.IsNullOrEmpty(c.SubscriptionPlan) && c.SubscriptionPlan != "free")
                .OrderByDescending(c => c.CreatedAt)
                .Take(limit)
                .Select(c => new SubscriptionDTO
                {
                    CustomerId = c.Id,
                    CustomerName = c.Name,
                    SubscriptionPlan = c.SubscriptionPlan,
                    ActivatedAt = c.CreatedAt,
                    StripeSubscriptionId = null, // À implémenter avec Stripe
                    Status = "active"
                })
                .ToListAsync();

            return Ok(subscriptions);
        }

        /// <summary>
        /// Récupère les tendances du nombre de clients (pour dashboard)
        /// </summary>
        [HttpGet("count/trend")]
        [AllowAnonymous]
        public async Task<ActionResult<TrendResponse>> GetCustomerCountTrend()
        {
            var now = DateTime.UtcNow;
            var firstDayThisMonth = new DateTime(now.Year, now.Month, 1);
            var firstDayLastMonth = firstDayThisMonth.AddMonths(-1);

            var total = await _context.Customers
                .Where(c => c.IsActive)
                .CountAsync();

            var thisMonth = await _context.Customers
                .Where(c => c.IsActive && c.CreatedAt >= firstDayThisMonth)
                .CountAsync();

            var lastMonth = await _context.Customers
                .Where(c => c.IsActive && c.CreatedAt >= firstDayLastMonth && c.CreatedAt < firstDayThisMonth)
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
