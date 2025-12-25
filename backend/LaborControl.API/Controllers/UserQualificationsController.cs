using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaborControl.API.Data;
using LaborControl.API.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace LaborControl.API.Controllers
{
    /// <summary>
    /// Gestion des qualifications des utilisateurs (attribuer, retirer, renouveler)
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserQualificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<UserQualificationsController> _logger;

        public UserQualificationsController(ApplicationDbContext context, ILogger<UserQualificationsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        private Guid GetCustomerId()
        {
            var customerIdClaim = User.FindFirst("CustomerId")?.Value;
            if (string.IsNullOrEmpty(customerIdClaim))
            {
                throw new UnauthorizedAccessException("CustomerId manquant dans le token");
            }
            return Guid.Parse(customerIdClaim);
        }

        // ========================================
        // GET: api/userqualifications/user/{userId}
        // Liste des qualifications d'un utilisateur
        // ========================================
        [HttpGet("user/{userId}")]
        public async Task<ActionResult<IEnumerable<UserQualificationDto>>> GetUserQualifications(
            Guid userId,
            [FromQuery] bool includeExpired = true)
        {
            try
            {
                var customerId = GetCustomerId();

                // Vérifier que l'utilisateur appartient au même customer
                var userExists = await _context.Users
                    .AnyAsync(u => u.Id == userId && u.CustomerId == customerId);

                if (!userExists)
                {
                    return NotFound(new { message = "Utilisateur introuvable" });
                }

                var query = _context.UserQualifications
                    .Include(uq => uq.Qualification)
                    .Where(uq => uq.UserId == userId && uq.IsActive);

                // Filtrer les qualifications expirées si demandé
                if (!includeExpired)
                {
                    query = query.Where(uq => !uq.ExpirationDate.HasValue || uq.ExpirationDate.Value >= DateTime.UtcNow);
                }

                var userQualifications = await query
                    .OrderBy(uq => uq.Qualification!.Category)
                    .ThenBy(uq => uq.Qualification!.Name)
                    .Select(uq => new UserQualificationDto
                    {
                        Id = uq.Id,
                        UserId = uq.UserId,
                        QualificationId = uq.QualificationId,
                        QualificationName = uq.Qualification!.Name,
                        QualificationCategory = uq.Qualification.Category,
                        QualificationCode = uq.Qualification.Code,
                        ObtainedDate = uq.ObtainedDate,
                        ExpirationDate = uq.ExpirationDate,
                        CertificateNumber = uq.CertificateNumber,
                        IssuingOrganization = uq.IssuingOrganization,
                        Notes = uq.Notes,
                        DocumentUrl = uq.DocumentUrl,
                        IsExpired = uq.ExpirationDate.HasValue && uq.ExpirationDate.Value < DateTime.UtcNow,
                        IsExpiringSoon = uq.ExpirationDate.HasValue &&
                                        uq.ExpirationDate.Value > DateTime.UtcNow &&
                                        uq.ExpirationDate.Value <= DateTime.UtcNow.AddDays(30)
                    })
                    .ToListAsync();

                return Ok(userQualifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des qualifications de l'utilisateur {UserId}", userId);
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }

        // ========================================
        // GET: api/userqualifications/expiring
        // Liste des qualifications qui expirent bientôt (dans les 30 jours)
        // ========================================
        [HttpGet("expiring")]
        public async Task<ActionResult<IEnumerable<ExpiringQualificationDto>>> GetExpiringQualifications(
            [FromQuery] int daysAhead = 30)
        {
            try
            {
                var customerId = GetCustomerId();
                var now = DateTime.UtcNow;
                var futureDate = now.AddDays(daysAhead);

                var expiringQualifications = await _context.UserQualifications
                    .Include(uq => uq.User)
                    .Include(uq => uq.Qualification)
                    .Where(uq => uq.User!.CustomerId == customerId &&
                                uq.IsActive &&
                                uq.ExpirationDate.HasValue &&
                                uq.ExpirationDate.Value > now &&
                                uq.ExpirationDate.Value <= futureDate)
                    .OrderBy(uq => uq.ExpirationDate)
                    .Select(uq => new ExpiringQualificationDto
                    {
                        UserQualificationId = uq.Id,
                        UserId = uq.UserId,
                        UserName = uq.User!.Prenom + " " + uq.User.Nom,
                        QualificationName = uq.Qualification!.Name,
                        QualificationCategory = uq.Qualification.Category,
                        ExpirationDate = uq.ExpirationDate!.Value,
                        DaysUntilExpiration = (int)(uq.ExpirationDate.Value - now).TotalDays,
                        CriticalityLevel = uq.Qualification.CriticalityLevel
                    })
                    .ToListAsync();

                return Ok(expiringQualifications);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des qualifications qui expirent");
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }

        // ========================================
        // POST: api/userqualifications
        // Attribuer une qualification à un utilisateur
        // ========================================
        [HttpPost]
        public async Task<ActionResult<UserQualificationDto>> AssignQualification([FromBody] AssignQualificationRequest request)
        {
            try
            {
                var customerId = GetCustomerId();

                // Vérifier que l'utilisateur existe et appartient au customer
                var user = await _context.Users
                    .FirstOrDefaultAsync(u => u.Id == request.UserId && u.CustomerId == customerId);

                if (user == null)
                {
                    return NotFound(new { message = "Utilisateur introuvable" });
                }

                // Vérifier que la qualification existe et appartient au customer
                var qualification = await _context.Qualifications
                    .FirstOrDefaultAsync(q => q.Id == request.QualificationId && q.CustomerId == customerId);

                if (qualification == null)
                {
                    return NotFound(new { message = "Qualification introuvable" });
                }

                // Vérifier si l'utilisateur a déjà cette qualification active
                var existing = await _context.UserQualifications
                    .FirstOrDefaultAsync(uq => uq.UserId == request.UserId &&
                                               uq.QualificationId == request.QualificationId &&
                                               uq.IsActive);

                if (existing != null)
                {
                    return BadRequest(new { message = "L'utilisateur possède déjà cette qualification active" });
                }

                // Calculer la date d'expiration si applicable
                DateTime? expirationDate = null;
                if (qualification.RequiresRenewal && qualification.ValidityPeriodMonths.HasValue)
                {
                    expirationDate = request.ObtainedDate.AddMonths(qualification.ValidityPeriodMonths.Value);
                }
                else if (request.ExpirationDate.HasValue)
                {
                    expirationDate = request.ExpirationDate.Value;
                }

                var userQualification = new UserQualification
                {
                    Id = Guid.NewGuid(),
                    UserId = request.UserId,
                    QualificationId = request.QualificationId,
                    ObtainedDate = request.ObtainedDate,
                    ExpirationDate = expirationDate,
                    CertificateNumber = request.CertificateNumber,
                    IssuingOrganization = request.IssuingOrganization ?? qualification.IssuingOrganization,
                    Notes = request.Notes,
                    DocumentUrl = request.DocumentUrl,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                _context.UserQualifications.Add(userQualification);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Qualification {QualificationName} attribuée à {UserName}",
                    qualification.Name, $"{user.Prenom} {user.Nom}");

                var dto = new UserQualificationDto
                {
                    Id = userQualification.Id,
                    UserId = userQualification.UserId,
                    QualificationId = userQualification.QualificationId,
                    QualificationName = qualification.Name,
                    QualificationCategory = qualification.Category,
                    QualificationCode = qualification.Code,
                    ObtainedDate = userQualification.ObtainedDate,
                    ExpirationDate = userQualification.ExpirationDate,
                    CertificateNumber = userQualification.CertificateNumber,
                    IssuingOrganization = userQualification.IssuingOrganization,
                    Notes = userQualification.Notes,
                    DocumentUrl = userQualification.DocumentUrl,
                    IsExpired = userQualification.IsExpired,
                    IsExpiringSoon = userQualification.IsExpiringSoon
                };

                return CreatedAtAction(nameof(GetUserQualifications), new { userId = request.UserId }, dto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de l'attribution de la qualification");
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }

        // ========================================
        // PUT: api/userqualifications/{id}
        // Modifier une qualification d'un utilisateur (ex: renouveler)
        // ========================================
        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateUserQualification(Guid id, [FromBody] UpdateUserQualificationRequest request)
        {
            try
            {
                var customerId = GetCustomerId();

                var userQualification = await _context.UserQualifications
                    .Include(uq => uq.User)
                    .FirstOrDefaultAsync(uq => uq.Id == id && uq.User!.CustomerId == customerId);

                if (userQualification == null)
                {
                    return NotFound(new { message = "Qualification utilisateur introuvable" });
                }

                userQualification.ObtainedDate = request.ObtainedDate;
                userQualification.ExpirationDate = request.ExpirationDate;
                userQualification.CertificateNumber = request.CertificateNumber;
                userQualification.IssuingOrganization = request.IssuingOrganization;
                userQualification.Notes = request.Notes;
                userQualification.DocumentUrl = request.DocumentUrl;
                userQualification.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Qualification utilisateur mise à jour: {Id}", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour de la qualification utilisateur {Id}", id);
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }

        // ========================================
        // DELETE: api/userqualifications/{id}
        // Retirer une qualification à un utilisateur
        // ========================================
        [HttpDelete("{id}")]
        public async Task<IActionResult> RemoveQualification(Guid id)
        {
            try
            {
                var customerId = GetCustomerId();

                var userQualification = await _context.UserQualifications
                    .Include(uq => uq.User)
                    .FirstOrDefaultAsync(uq => uq.Id == id && uq.User!.CustomerId == customerId);

                if (userQualification == null)
                {
                    return NotFound(new { message = "Qualification utilisateur introuvable" });
                }

                // Soft delete
                userQualification.IsActive = false;
                userQualification.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Qualification retirée: {Id}", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors du retrait de la qualification {Id}", id);
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }
    }

    // ========================================
    // DTOs
    // ========================================

    public class UserQualificationDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public Guid QualificationId { get; set; }
        public string QualificationName { get; set; } = string.Empty;
        public string QualificationCategory { get; set; } = string.Empty;
        public string? QualificationCode { get; set; }
        public DateTime ObtainedDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string? CertificateNumber { get; set; }
        public string? IssuingOrganization { get; set; }
        public string? Notes { get; set; }
        public string? DocumentUrl { get; set; }
        public bool IsExpired { get; set; }
        public bool IsExpiringSoon { get; set; }
    }

    public class ExpiringQualificationDto
    {
        public Guid UserQualificationId { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string QualificationName { get; set; } = string.Empty;
        public string QualificationCategory { get; set; } = string.Empty;
        public DateTime ExpirationDate { get; set; }
        public int DaysUntilExpiration { get; set; }
        public int CriticalityLevel { get; set; }
    }

    public class AssignQualificationRequest
    {
        public Guid UserId { get; set; }
        public Guid QualificationId { get; set; }
        public DateTime ObtainedDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string? CertificateNumber { get; set; }
        public string? IssuingOrganization { get; set; }
        public string? Notes { get; set; }
        public string? DocumentUrl { get; set; }
    }

    public class UpdateUserQualificationRequest
    {
        public DateTime ObtainedDate { get; set; }
        public DateTime? ExpirationDate { get; set; }
        public string? CertificateNumber { get; set; }
        public string? IssuingOrganization { get; set; }
        public string? Notes { get; set; }
        public string? DocumentUrl { get; set; }
    }
}
