using LaborControl.API.Data;
using LaborControl.API.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaborControl.API.Controllers
{
    [ApiController]
    [Route("api/seed")]
    public class SeedDataController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public SeedDataController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("reset")]
        public async Task<IActionResult> ResetData()
        {
            _context.TaskExecutions.RemoveRange(_context.TaskExecutions);
            _context.ScheduledTasks.RemoveRange(_context.ScheduledTasks);
            _context.ControlPoints.RemoveRange(_context.ControlPoints);
            _context.RfidChips.RemoveRange(_context.RfidChips);
            _context.Users.RemoveRange(_context.Users);
            _context.Customers.RemoveRange(_context.Customers);
            await _context.SaveChangesAsync();

            var customer = new Customer
            {
                Id = Guid.Parse("2c7fad4f-2c46-446d-b920-6608fb341bb6"),
                Name = "EHPAD Les Roses",
                SubscriptionPlan = "FREE",
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Customers.Add(customer);

            var user = new User
            {
                Id = Guid.Parse("945169bc-97cf-420a-8ddf-bf064dadd4bc"),
                Email = "jean.dupont@ehpad-roses.fr",
                PasswordHash = "Loulou",
                Role = "TECHNICIAN",
                CustomerId = customer.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _context.Users.Add(user);

            // ⚠️ UID RÉEL scanné depuis votre téléphone
            var chips = new List<RfidChip>
            {
                new RfidChip { Id = Guid.NewGuid(), ChipId = "RFID-2025-CH101", Uid = "045A9B12361E91", CustomerId = customer.Id, Status = "ACTIVE", Checksum = "abc123checksum101", ActivationDate = DateTime.UtcNow },
                new RfidChip { Id = Guid.NewGuid(), ChipId = "RFID-2025-CH102", Uid = "A1B2C3D5", CustomerId = customer.Id, Status = "ACTIVE", Checksum = "abc123checksum102", ActivationDate = DateTime.UtcNow },
                new RfidChip { Id = Guid.NewGuid(), ChipId = "RFID-2025-CH103", Uid = "A1B2C3D6", CustomerId = customer.Id, Status = "ACTIVE", Checksum = "abc123checksum103", ActivationDate = DateTime.UtcNow }
            };
            _context.RfidChips.AddRange(chips);

            var points = new List<ControlPoint>
            {
                new ControlPoint { Id = Guid.Parse("ca8cead0-b555-4c06-ba41-d67d72107edf"), Name = "Chambre 101", LocationDescription = "1er étage, couloir gauche", CustomerId = customer.Id, RfidChipId = chips[0].Id, IsActive = true, CreatedAt = DateTime.UtcNow },
                new ControlPoint { Id = Guid.NewGuid(), Name = "Chambre 102", LocationDescription = "1er étage, couloir gauche", CustomerId = customer.Id, RfidChipId = chips[1].Id, IsActive = true, CreatedAt = DateTime.UtcNow },
                new ControlPoint { Id = Guid.NewGuid(), Name = "Chambre 103", LocationDescription = "1er étage, couloir droit", CustomerId = customer.Id, RfidChipId = chips[2].Id, IsActive = true, CreatedAt = DateTime.UtcNow }
            };
            _context.ControlPoints.AddRange(points);

            var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
            var tasks = new List<ScheduledTask>
            {
                new ScheduledTask { Id = Guid.NewGuid(), UserId = user.Id, ControlPointId = points[0].Id, CustomerId = customer.Id, ScheduledDate = today, ScheduledTimeStart = TimeSpan.Parse("08:00"), ScheduledTimeEnd = TimeSpan.Parse("09:00"), Status = "PENDING", Recurrence = "DAILY", CreatedAt = DateTime.UtcNow },
                new ScheduledTask { Id = Guid.NewGuid(), UserId = user.Id, ControlPointId = points[1].Id, CustomerId = customer.Id, ScheduledDate = today, ScheduledTimeStart = TimeSpan.Parse("10:00"), ScheduledTimeEnd = TimeSpan.Parse("11:00"), Status = "PENDING", Recurrence = "DAILY", CreatedAt = DateTime.UtcNow },
                new ScheduledTask { Id = Guid.NewGuid(), UserId = user.Id, ControlPointId = points[2].Id, CustomerId = customer.Id, ScheduledDate = today, ScheduledTimeStart = TimeSpan.Parse("14:00"), ScheduledTimeEnd = TimeSpan.Parse("15:00"), Status = "PENDING", Recurrence = "DAILY", CreatedAt = DateTime.UtcNow }
            };
            _context.ScheduledTasks.AddRange(tasks);

            await _context.SaveChangesAsync();

            return Ok(new { message = "✅ OK", customers = 1, users = 1, chips = 3, points = 3, tasks = 3 });
        }

        [HttpPost("create-admin")]
        public async Task<IActionResult> CreateAdmin()
        {
            // Vérifier si l'admin existe déjà
            var existingAdmin = await _context.Users.FirstOrDefaultAsync(u => u.Email == "admin@laborcontrol.com");
            if (existingAdmin != null)
            {
                return Ok(new { message = "Admin existe déjà", userId = existingAdmin.Id });
            }

            // Récupérer le premier customer
            var customer = await _context.Customers.FirstOrDefaultAsync();
            if (customer == null)
            {
                return BadRequest(new { message = "Aucun customer trouvé. Exécutez /api/seed/reset d'abord" });
            }

            // Créer l'admin avec mot de passe hashé
            var admin = new User
            {
                Id = Guid.NewGuid(),
                Email = "admin@laborcontrol.com",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@2025"),
                Nom = "Admin",
                Prenom = "Système",
                Tel = "0600000000",
                Service = "Direction",
                Fonction = "Administrateur",
                Role = "ADMIN",
                CustomerId = customer.Id,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Users.Add(admin);
            await _context.SaveChangesAsync();

            return Ok(new {
                message = "✅ Admin créé avec succès",
                email = "admin@laborcontrol.com",
                password = "Admin@2025",
                userId = admin.Id
            });
        }
    }
}
