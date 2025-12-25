using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaborControl.API.Data;
using LaborControl.API.Models;
using LaborControl.API.DTOs;

namespace LaborControl.API.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class MaterialsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<MaterialsController> _logger;

        public MaterialsController(ApplicationDbContext context, ILogger<MaterialsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private Guid GetCustomerId()
        {
            var customerIdClaim = HttpContext.User.FindFirst("CustomerId")?.Value;
            return Guid.TryParse(customerIdClaim, out var customerId) ? customerId : Guid.Empty;
        }

        private Guid GetUserId()
        {
            var userIdClaim = HttpContext.User.FindFirst("UserId")?.Value;
            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }

        // GET: api/materials
        [HttpGet]
        public async Task<ActionResult<IEnumerable<MaterialDto>>> GetMaterials(
            [FromQuery] string? status = null,
            [FromQuery] string? grade = null,
            [FromQuery] bool? isBlocked = null)
        {
            try
            {
                var customerId = GetCustomerId();

                var query = _context.Materials
                    .Where(m => m.CustomerId == customerId && m.IsActive);

                if (!string.IsNullOrEmpty(status))
                    query = query.Where(m => m.Status == status);

                if (!string.IsNullOrEmpty(grade))
                    query = query.Where(m => m.Grade == grade);

                if (isBlocked.HasValue)
                    query = query.Where(m => m.IsBlocked == isBlocked.Value);

                var materials = await query
                    .AsNoTracking()
                    .OrderByDescending(m => m.CreatedAt)
                    .Select(m => new MaterialDto
                    {
                        Id = m.Id,
                        Reference = m.Reference,
                        Name = m.Name,
                        Grade = m.Grade,
                        Specification = m.Specification,
                        HeatNumber = m.HeatNumber,
                        BatchNumber = m.BatchNumber,
                        Supplier = m.Supplier,
                        CertificateNumber = m.CertificateNumber,
                        ReceiptDate = m.ReceiptDate,
                        Quantity = m.Quantity,
                        Unit = m.Unit,
                        Status = m.Status,
                        IsBlocked = m.IsBlocked,
                        IsCCPUValidated = m.CCPUValidatorId.HasValue,
                        CreatedAt = m.CreatedAt
                    })
                    .ToListAsync();

                return Ok(materials);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des matériaux");
                return StatusCode(500, "Erreur serveur");
            }
        }

        // GET: api/materials/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<MaterialDetailDto>> GetMaterial(Guid id)
        {
            try
            {
                var customerId = GetCustomerId();

                var material = await _context.Materials
                    .Include(m => m.CCPUValidator)
                    .Include(m => m.Subcontractor)
                    .Include(m => m.NonConformities.Where(nc => nc.IsActive))
                        .ThenInclude(nc => nc.CreatedBy)
                    .FirstOrDefaultAsync(m => m.Id == id && m.CustomerId == customerId);

                if (material == null)
                    return NotFound("Matériau introuvable");

                var dto = new MaterialDetailDto
                {
                    Id = material.Id,
                    Reference = material.Reference,
                    Name = material.Name,
                    Grade = material.Grade,
                    Specification = material.Specification,
                    HeatNumber = material.HeatNumber,
                    BatchNumber = material.BatchNumber,
                    Supplier = material.Supplier,
                    CertificateNumber = material.CertificateNumber,
                    ReceiptDate = material.ReceiptDate,
                    Quantity = material.Quantity,
                    Unit = material.Unit,
                    Status = material.Status,
                    IsBlocked = material.IsBlocked,
                    IsCCPUValidated = material.CCPUValidatorId.HasValue,
                    Dimensions = material.Dimensions,
                    CertificateFilePath = material.CertificateFilePath,
                    CCPUValidatorId = material.CCPUValidatorId,
                    CCPUValidatorName = material.CCPUValidator != null
                        ? $"{material.CCPUValidator.Prenom} {material.CCPUValidator.Nom}"
                        : null,
                    CCPUValidationDate = material.CCPUValidationDate,
                    CCPUComments = material.CCPUComments,
                    BlockReason = material.BlockReason,
                    SubcontractorId = material.SubcontractorId,
                    SubcontractorName = material.Subcontractor != null
                        ? $"{material.Subcontractor.Prenom} {material.Subcontractor.Nom}"
                        : null,
                    StorageLocation = material.StorageLocation,
                    CreatedAt = material.CreatedAt,
                    UpdatedAt = material.UpdatedAt,
                    NonConformities = material.NonConformities.Select(nc => new NonConformityDto
                    {
                        Id = nc.Id,
                        Reference = nc.Reference,
                        Title = nc.Title,
                        Type = nc.Type,
                        Severity = nc.Severity,
                        MaterialId = nc.MaterialId,
                        MaterialReference = material.Reference,
                        Status = nc.Status,
                        CreatedById = nc.CreatedById,
                        CreatedByName = nc.CreatedBy != null
                            ? $"{nc.CreatedBy.Prenom} {nc.CreatedBy.Nom}"
                            : "",
                        DetectionDate = nc.DetectionDate,
                        DueDate = nc.DueDate,
                        RequiresRecontrol = nc.RequiresRecontrol,
                        CreatedAt = nc.CreatedAt
                    }).ToList()
                };

                return Ok(dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du matériau {MaterialId}", id);
                return StatusCode(500, "Erreur serveur");
            }
        }

        // POST: api/materials
        [HttpPost]
        public async Task<ActionResult<MaterialDto>> CreateMaterial([FromBody] CreateMaterialRequest request)
        {
            try
            {
                var customerId = GetCustomerId();

                // Vérifier unicité de la référence
                var exists = await _context.Materials.AnyAsync(m =>
                    m.CustomerId == customerId && m.Reference == request.Reference);
                if (exists)
                    return BadRequest("Un matériau avec cette référence existe déjà");

                var material = new Material
                {
                    Id = Guid.NewGuid(),
                    CustomerId = customerId,
                    Reference = request.Reference,
                    Name = request.Name,
                    Grade = request.Grade,
                    Specification = request.Specification,
                    HeatNumber = request.HeatNumber,
                    BatchNumber = request.BatchNumber,
                    Supplier = request.Supplier,
                    CertificateNumber = request.CertificateNumber,
                    ReceiptDate = request.ReceiptDate,
                    Quantity = request.Quantity,
                    Unit = request.Unit,
                    Dimensions = request.Dimensions,
                    SubcontractorId = request.SubcontractorId,
                    StorageLocation = request.StorageLocation,
                    Status = "PENDING_VALIDATION",
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Materials.Add(material);
                await _context.SaveChangesAsync();

                var dto = new MaterialDto
                {
                    Id = material.Id,
                    Reference = material.Reference,
                    Name = material.Name,
                    Grade = material.Grade,
                    Status = material.Status,
                    CreatedAt = material.CreatedAt
                };

                return CreatedAtAction(nameof(GetMaterial), new { id = material.Id }, dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du matériau");
                return StatusCode(500, "Erreur serveur");
            }
        }

        // PUT: api/materials/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateMaterial(Guid id, [FromBody] UpdateMaterialRequest request)
        {
            try
            {
                var customerId = GetCustomerId();

                var material = await _context.Materials.FirstOrDefaultAsync(m =>
                    m.Id == id && m.CustomerId == customerId);

                if (material == null)
                    return NotFound("Matériau introuvable");

                if (material.IsBlocked)
                    return BadRequest("Ce matériau est verrouillé");

                if (material.Status == "VALIDATED")
                    return BadRequest("Impossible de modifier un matériau validé");

                material.Name = request.Name;
                material.Grade = request.Grade;
                material.Specification = request.Specification;
                material.HeatNumber = request.HeatNumber;
                material.BatchNumber = request.BatchNumber;
                material.Supplier = request.Supplier;
                material.CertificateNumber = request.CertificateNumber;
                material.ReceiptDate = request.ReceiptDate;
                material.Quantity = request.Quantity;
                material.Unit = request.Unit;
                material.Dimensions = request.Dimensions;
                material.SubcontractorId = request.SubcontractorId;
                material.StorageLocation = request.StorageLocation;
                material.Status = request.Status;
                material.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du matériau {MaterialId}", id);
                return StatusCode(500, "Erreur serveur");
            }
        }

        // POST: api/materials/{id}/ccpu-validation
        [HttpPost("{id}/ccpu-validation")]
        public async Task<IActionResult> CCPUValidation(Guid id, [FromBody] MaterialCCPUValidationRequest request)
        {
            try
            {
                var customerId = GetCustomerId();
                var userId = GetUserId();

                var material = await _context.Materials.FirstOrDefaultAsync(m =>
                    m.Id == id && m.CustomerId == customerId);

                if (material == null)
                    return NotFound("Matériau introuvable");

                // Vérifier les droits CCPU
                var user = await _context.Users.FindAsync(userId);
                if (user == null || !user.CanValidateAsCCPU)
                    return Forbid("Vous n'avez pas les droits CCPU");

                if (request.Approve)
                {
                    material.CCPUValidatorId = userId;
                    material.CCPUValidationDate = DateTime.UtcNow;
                    material.CCPUComments = request.Comments;
                    material.Status = "VALIDATED";
                    material.IsBlocked = false;
                    material.BlockReason = null;
                }
                else
                {
                    material.IsBlocked = true;
                    material.BlockReason = request.BlockReason ?? "Refusé par CCPU";
                    material.CCPUComments = request.Comments;
                    material.Status = "REJECTED";
                }

                material.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Ok(new {
                    Message = request.Approve ? "Matériau validé par CCPU" : "Matériau refusé par CCPU",
                    Status = material.Status
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la validation CCPU du matériau {MaterialId}", id);
                return StatusCode(500, "Erreur serveur");
            }
        }

        // DELETE: api/materials/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteMaterial(Guid id)
        {
            try
            {
                var customerId = GetCustomerId();

                var material = await _context.Materials
                    .Include(m => m.NonConformities)
                    .FirstOrDefaultAsync(m => m.Id == id && m.CustomerId == customerId);

                if (material == null)
                    return NotFound("Matériau introuvable");

                // Vérifier qu'il n'y a pas de FNC actives
                if (material.NonConformities.Any(nc => nc.IsActive && nc.Status != "CLOSED"))
                    return BadRequest("Impossible de supprimer un matériau avec des FNC ouvertes");

                // Soft delete
                material.IsActive = false;
                material.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du matériau {MaterialId}", id);
                return StatusCode(500, "Erreur serveur");
            }
        }
    }
}
