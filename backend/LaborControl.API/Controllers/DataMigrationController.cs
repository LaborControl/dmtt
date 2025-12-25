using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaborControl.API.Data;
using LaborControl.API.Models;

namespace LaborControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DataMigrationController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public DataMigrationController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost("migrate-control-points")]
        public async Task<ActionResult> MigrateControlPointsToAssets()
        {
            // Récupérer tous les ControlPoints sans Asset
            var orphanControlPoints = await _context.ControlPoints
                .Where(cp => cp.AssetId == null)
                .Include(cp => cp.Customer)
                .ToListAsync();

            if (!orphanControlPoints.Any())
            {
                return Ok(new { message = "Aucun ControlPoint à migrer. Tous ont déjà un Asset." });
            }

            // Grouper par Customer
            var groupedByCustomer = orphanControlPoints.GroupBy(cp => cp.CustomerId);
            
            var migrationResults = new List<object>();

            foreach (var customerGroup in groupedByCustomer)
            {
                var customerId = customerGroup.Key;
                var customer = await _context.Customers.FindAsync(customerId);
                
                if (customer == null) continue;

                // 1. Créer ou récupérer le Site par défaut
                var defaultSite = await _context.Sites
                    .FirstOrDefaultAsync(s => s.CustomerId == customerId && s.Name == "Site Principal");

                if (defaultSite == null)
                {
                    defaultSite = new Site
                    {
                        Id = Guid.NewGuid(),
                        CustomerId = customerId,
                        Name = "Site Principal",
                        Code = "SITE-001",
                        Address = "À compléter",
                        City = "À compléter",
                        Country = "France",
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Sites.Add(defaultSite);
                    await _context.SaveChangesAsync();
                }

                // 2. Créer ou récupérer la Zone par défaut
                var defaultZone = await _context.Zones
                    .FirstOrDefaultAsync(z => z.SiteId == defaultSite.Id && z.Name == "Zone Principale");

                if (defaultZone == null)
                {
                    // TODO: Sector supprimé du modèle Customer - maintenant au niveau des Teams
                    // On utilise "PRODUCTION" comme type par défaut pour la migration
                    defaultZone = new Zone
                    {
                        Id = Guid.NewGuid(),
                        SiteId = defaultSite.Id,
                        Name = "Zone Principale",
                        Code = "ZONE-001",
                        Type = "PRODUCTION", // Type par défaut (avant: customer.Sector == "EHPAD" ? "BUILDING" : "PRODUCTION")
                        Description = "Zone créée automatiquement lors de la migration",
                        Level = 0,
                        DisplayOrder = 0,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Zones.Add(defaultZone);
                    await _context.SaveChangesAsync();
                }

                // 3. Créer un Asset pour chaque ControlPoint
                foreach (var controlPoint in customerGroup)
                {
                    // TODO: Sector supprimé du modèle Customer - maintenant au niveau des Teams
                    // On utilise "GENERIC" comme type par défaut pour la migration
                    string assetType = "GENERIC"; // Type par défaut (avant: basé sur customer.Sector)

                    // Créer l'Asset
                    var asset = new Asset
                    {
                        Id = Guid.NewGuid(),
                        ZoneId = defaultZone.Id,
                        Name = controlPoint.Name,
                        Code = controlPoint.Code ?? $"ASSET-{Guid.NewGuid().ToString().Substring(0, 8)}",
                        Type = assetType,
                        Category = "STANDARD",
                        Status = "OPERATIONAL",
                        Level = 0,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow
                    };
                    _context.Assets.Add(asset);
                    await _context.SaveChangesAsync();

                    // 4. Lier le ControlPoint à cet Asset
                    controlPoint.AssetId = asset.Id;
                    controlPoint.UpdatedAt = DateTime.UtcNow;
                    _context.ControlPoints.Update(controlPoint);
                    await _context.SaveChangesAsync();

                    migrationResults.Add(new
                    {
                        ControlPoint = controlPoint.Name,
                        NewAsset = asset.Name,
                        Zone = defaultZone.Name,
                        Site = defaultSite.Name
                    });
                }
            }

            return Ok(new
            {
                message = $"Migration terminée : {migrationResults.Count} ControlPoints migrés",
                details = migrationResults
            });
        }

        [HttpGet("migration-status")]
        public async Task<ActionResult> GetMigrationStatus()
        {
            var totalControlPoints = await _context.ControlPoints.CountAsync();
            var migratedControlPoints = await _context.ControlPoints
                .Where(cp => cp.AssetId != null)
                .CountAsync();
            var nonMigratedControlPoints = await _context.ControlPoints
                .Where(cp => cp.AssetId == null)
                .CountAsync();

            var customers = await _context.Customers.CountAsync();
            var sites = await _context.Sites.CountAsync();
            var zones = await _context.Zones.CountAsync();
            var assets = await _context.Assets.CountAsync();

            return Ok(new
            {
                ControlPoints = new
                {
                    Total = totalControlPoints,
                    Migrated = migratedControlPoints,
                    NonMigrated = nonMigratedControlPoints
                },
                NewArchitecture = new
                {
                    Customers = customers,
                    Sites = sites,
                    Zones = zones,
                    Assets = assets
                }
            });
        }
    }
}