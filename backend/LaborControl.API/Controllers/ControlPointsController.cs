using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaborControl.API.Data;
using LaborControl.API.Models;
using LaborControl.API.DTOs;
using System.Security.Claims;

namespace LaborControl.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ControlPointsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<ControlPointsController> _logger;

        public ControlPointsController(ApplicationDbContext context, ILogger<ControlPointsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private Guid GetCustomerId()
        {
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;
            return Guid.Parse(customerIdClaim!);
        }

        // GET: api/controlpoints
        [HttpGet]
        public async Task<ActionResult<IEnumerable<ControlPointResponse>>> GetControlPoints()
        {
            var customerId = GetCustomerId();

            var points = await _context.ControlPoints
                .Where(p => p.CustomerId == customerId && p.IsActive)
                .Include(p => p.RfidChip)
                .Include(p => p.Zone)
                .Include(p => p.Asset)
                    .ThenInclude(a => a.Zone)
                .Select(p => new ControlPointResponse
                {
                    Id = p.Id,
                    Code = p.Code,
                    Name = p.Name,
                    Description = p.Description,
                    LocationDescription = p.LocationDescription,
                    RfidChipId = p.RfidChipId,
                    RfidChipCode = p.RfidChip == null ? null : p.RfidChip.ChipId,
                    ZoneId = p.ZoneId,
                    ZoneName = p.Zone == null ? null : p.Zone.Name,
                    // SiteId peut venir soit de la Zone directe, soit de la Zone de l'Asset
                    SiteId = p.Zone != null ? p.Zone.SiteId : (p.Asset != null && p.Asset.Zone != null ? p.Asset.Zone.SiteId : null),
                    AssetId = p.AssetId,
                    AssetName = p.Asset == null ? null : p.Asset.Name,
                    CustomerId = p.CustomerId,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    IsActive = p.IsActive
                })
                .ToListAsync();

            // Log pour debug
            foreach (var point in points)
            {
                _logger.LogInformation($"Point: {point.Name}, ZoneId: {point.ZoneId}, ZoneName: {point.ZoneName}, SiteId: {point.SiteId}");
            }

            return Ok(points);
        }

        // GET: api/controlpoints/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<ControlPoint>> GetControlPoint(Guid id)
        {
            var customerId = GetCustomerId();

            var point = await _context.ControlPoints
                .Include(p => p.RfidChip)
                .FirstOrDefaultAsync(p => p.Id == id && p.CustomerId == customerId);

            if (point == null)
                return NotFound();

            return Ok(point);
        }

        // POST: api/controlpoints
        [HttpPost]
        public async Task<ActionResult<ControlPointResponse>> CreateControlPoint(CreateControlPointRequest request)
        {
            var customerId = GetCustomerId();

            // Validation: au moins Zone OU Asset doit être renseigné
            if (!request.ZoneId.HasValue && !request.AssetId.HasValue)
            {
                return BadRequest(new { message = "Au moins une Zone ou un Asset doit être spécifié" });
            }

            var point = new ControlPoint
            {
                Id = Guid.NewGuid(),
                Code = request.Code,
                Name = request.Name,
                Description = request.Description,
                LocationDescription = request.LocationDescription,
                ZoneId = request.ZoneId,
                AssetId = request.AssetId,
                CustomerId = customerId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.ControlPoints.Add(point);
            await _context.SaveChangesAsync();

            // Retourner avec les noms de Zone/Asset
            var response = await _context.ControlPoints
                .Where(p => p.Id == point.Id)
                .Include(p => p.Zone)
                .Include(p => p.Asset)
                    .ThenInclude(a => a.Zone)
                .Select(p => new ControlPointResponse
                {
                    Id = p.Id,
                    Code = p.Code,
                    Name = p.Name,
                    Description = p.Description,
                    LocationDescription = p.LocationDescription,
                    ZoneId = p.ZoneId,
                    ZoneName = p.Zone == null ? null : p.Zone.Name,
                    // SiteId peut venir soit de la Zone directe, soit de la Zone de l'Asset
                    SiteId = p.Zone != null ? p.Zone.SiteId : (p.Asset != null && p.Asset.Zone != null ? p.Asset.Zone.SiteId : null),
                    AssetId = p.AssetId,
                    AssetName = p.Asset == null ? null : p.Asset.Name,
                    CustomerId = p.CustomerId,
                    CreatedAt = p.CreatedAt,
                    UpdatedAt = p.UpdatedAt,
                    IsActive = p.IsActive
                })
                .FirstOrDefaultAsync();

            return CreatedAtAction(nameof(GetControlPoint), new { id = point.Id }, response);
        }

        // PUT: api/controlpoints/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateControlPoint(Guid id, UpdateControlPointRequest request)
        {
            var customerId = GetCustomerId();

            // Validation: au moins Zone OU Asset doit être renseigné
            if (!request.ZoneId.HasValue && !request.AssetId.HasValue)
            {
                return BadRequest(new { message = "Au moins une Zone ou un Asset doit être spécifié" });
            }

            var existingPoint = await _context.ControlPoints
                .FirstOrDefaultAsync(p => p.Id == id && p.CustomerId == customerId);

            if (existingPoint == null)
                return NotFound();

            existingPoint.Code = request.Code;
            existingPoint.Name = request.Name;
            existingPoint.Description = request.Description;
            existingPoint.LocationDescription = request.LocationDescription;
            existingPoint.ZoneId = request.ZoneId;
            existingPoint.AssetId = request.AssetId;
            existingPoint.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return NoContent();
        }

        // DELETE: api/controlpoints/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteControlPoint(Guid id)
        {
            var customerId = GetCustomerId();

            var point = await _context.ControlPoints
                .FirstOrDefaultAsync(p => p.Id == id && p.CustomerId == customerId);

            if (point == null)
                return NotFound();

            point.IsActive = false;
            await _context.SaveChangesAsync();

            return NoContent();
        }

        /// <summary>
        /// [MOBILE APP] Affecter une puce RFID à un point de contrôle
        /// Utilisé par l'app mobile lors du scan d'une puce
        /// </summary>
        [HttpPost("{id}/assign-rfid")]
        public async Task<IActionResult> AssignRfidChip(Guid id, [FromBody] AssignRfidChipRequest request)
        {
            var customerId = GetCustomerId();

            // Vérifier que le point de contrôle existe et appartient au client
            var controlPoint = await _context.ControlPoints
                .FirstOrDefaultAsync(p => p.Id == id && p.CustomerId == customerId && p.IsActive);

            if (controlPoint == null)
                return NotFound(new { message = "Point de contrôle introuvable" });

            // Vérifier que la puce existe et appartient au client
            var rfidChip = await _context.RfidChips
                .FirstOrDefaultAsync(c => c.Id == request.RfidChipId && c.CustomerId == customerId);

            if (rfidChip == null)
                return NotFound(new { message = "Puce RFID introuvable" });

            // Vérifier que la puce a bien été reçue
            if (rfidChip.Status != "RECEIVED" && rfidChip.Status != "ACTIVE")
            {
                return BadRequest(new { message = $"La puce doit être réceptionnée avant d'être affectée (statut actuel: {rfidChip.Status})" });
            }

            // Vérifier si la puce n'est pas déjà affectée ailleurs
            var existingAssignment = await _context.ControlPoints
                .FirstOrDefaultAsync(p => p.RfidChipId == request.RfidChipId && p.IsActive && p.Id != id);

            if (existingAssignment != null)
            {
                return BadRequest(new { message = $"Cette puce est déjà affectée au point de contrôle '{existingAssignment.Name}'" });
            }

            // Affecter la puce au point de contrôle
            controlPoint.RfidChipId = request.RfidChipId;
            controlPoint.UpdatedAt = DateTime.UtcNow;

            // Marquer la puce comme ACTIVE
            rfidChip.Status = "ACTIVE";

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Puce RFID {rfidChip.ChipId} affectée au point de contrôle {controlPoint.Name}");

            return Ok(new { message = "Puce affectée avec succès", controlPointName = controlPoint.Name, chipCode = rfidChip.ChipId });
        }

        /// <summary>
        /// [MOBILE APP] Désaffecter une puce RFID d'un point de contrôle
        /// </summary>
        [HttpPost("{id}/unassign-rfid")]
        public async Task<IActionResult> UnassignRfidChip(Guid id)
        {
            var customerId = GetCustomerId();

            var controlPoint = await _context.ControlPoints
                .Include(p => p.RfidChip)
                .FirstOrDefaultAsync(p => p.Id == id && p.CustomerId == customerId && p.IsActive);

            if (controlPoint == null)
                return NotFound(new { message = "Point de contrôle introuvable" });

            if (controlPoint.RfidChipId == null)
                return BadRequest(new { message = "Aucune puce affectée à ce point de contrôle" });

            var rfidChip = controlPoint.RfidChip;

            // Désaffecter la puce
            controlPoint.RfidChipId = null;
            controlPoint.UpdatedAt = DateTime.UtcNow;

            // Remettre la puce en RECEIVED
            if (rfidChip != null)
            {
                rfidChip.Status = "RECEIVED";
            }

            await _context.SaveChangesAsync();

            _logger.LogInformation($"Puce RFID {(rfidChip == null ? "null" : rfidChip.ChipId)} désaffectée du point de contrôle {controlPoint.Name}");

            return Ok(new { message = "Puce désaffectée avec succès" });
        }
    }
}
