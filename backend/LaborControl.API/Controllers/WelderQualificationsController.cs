using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaborControl.API.Data;
using LaborControl.API.Models;
using LaborControl.API.DTOs;
using LaborControl.API.Services.AI;
using System.Security.Claims;

namespace LaborControl.API.Controllers
{
    /// <summary>
    /// Controller for Welder Qualifications management
    /// Includes AI pre-validation via Gemini OCR
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class WelderQualificationsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IGeminiOCRService _geminiService;
        private readonly ILogger<WelderQualificationsController> _logger;

        public WelderQualificationsController(
            ApplicationDbContext context,
            IGeminiOCRService geminiService,
            ILogger<WelderQualificationsController> logger)
        {
            _context = context;
            _geminiService = geminiService;
            _logger = logger;
        }

        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        private Guid? GetCustomerId() => User.FindFirstValue("CustomerId") is string cid ? Guid.Parse(cid) : null;

        /// <summary>
        /// Get current user's welder qualifications
        /// </summary>
        [HttpGet("my-qualifications")]
        public async Task<ActionResult<IEnumerable<WelderQualificationDto>>> GetMyQualifications()
        {
            var userId = GetUserId();

            var qualifications = await _context.WelderQualifications
                .Where(q => q.UserId == userId && q.IsActive)
                .OrderByDescending(q => q.ExpirationDate)
                .Select(q => new WelderQualificationDto
                {
                    Id = q.Id,
                    QualificationNumber = q.QualificationNumber,
                    WeldingProcess = q.WeldingProcess,
                    QualificationStandard = q.QualificationStandard,
                    QualifiedPositions = q.QualifiedPositions,
                    QualifiedMaterials = q.QualifiedMaterials,
                    ThicknessRange = q.ThicknessRange,
                    DiameterRange = q.DiameterRange,
                    IssueDate = q.IssueDate,
                    ExpirationDate = q.ExpirationDate,
                    Status = GetQualificationStatus(q.ExpirationDate),
                    AiPreValidated = q.PreValidatedByAI,
                    AiConfidenceScore = q.AIConfidenceScore,
                    CertifyingBody = q.CertifyingBody
                })
                .ToListAsync();

            return Ok(qualifications);
        }

        /// <summary>
        /// Get all qualifications (admin/manager view)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<WelderQualificationDto>>> GetAllQualifications(
            [FromQuery] Guid? userId = null,
            [FromQuery] string? process = null,
            [FromQuery] bool? expiringSoon = null)
        {
            var customerId = GetCustomerId();
            if (customerId == null)
                return Forbid();

            var query = _context.WelderQualifications
                .Include(q => q.User)
                .Where(q => q.CustomerId == customerId && q.IsActive);

            if (userId.HasValue)
                query = query.Where(q => q.UserId == userId.Value);

            if (!string.IsNullOrEmpty(process))
                query = query.Where(q => q.WeldingProcess != null && q.WeldingProcess.Contains(process));

            if (expiringSoon == true)
            {
                var thirtyDaysFromNow = DateTime.UtcNow.AddDays(30);
                query = query.Where(q => q.ExpirationDate <= thirtyDaysFromNow && q.ExpirationDate > DateTime.UtcNow);
            }

            var qualifications = await query
                .OrderBy(q => q.ExpirationDate)
                .Select(q => new WelderQualificationDto
                {
                    Id = q.Id,
                    QualificationNumber = q.QualificationNumber,
                    WelderId = q.UserId,
                    WelderName = q.User != null ? $"{q.User.Prenom} {q.User.Nom}" : null,
                    WeldingProcess = q.WeldingProcess,
                    QualificationStandard = q.QualificationStandard,
                    QualifiedPositions = q.QualifiedPositions,
                    QualifiedMaterials = q.QualifiedMaterials,
                    ThicknessRange = q.ThicknessRange,
                    DiameterRange = q.DiameterRange,
                    IssueDate = q.IssueDate,
                    ExpirationDate = q.ExpirationDate,
                    Status = GetQualificationStatus(q.ExpirationDate),
                    AiPreValidated = q.PreValidatedByAI,
                    AiConfidenceScore = q.AIConfidenceScore,
                    CertifyingBody = q.CertifyingBody
                })
                .ToListAsync();

            return Ok(qualifications);
        }

        /// <summary>
        /// Get qualification by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<WelderQualificationDto>> GetQualification(Guid id)
        {
            var qualification = await _context.WelderQualifications
                .Include(q => q.User)
                .FirstOrDefaultAsync(q => q.Id == id && q.IsActive);

            if (qualification == null)
                return NotFound();

            return Ok(new WelderQualificationDto
            {
                Id = qualification.Id,
                QualificationNumber = qualification.QualificationNumber,
                WelderId = qualification.UserId,
                WelderName = qualification.User != null ? $"{qualification.User.Prenom} {qualification.User.Nom}" : null,
                WeldingProcess = qualification.WeldingProcess,
                QualificationStandard = qualification.QualificationStandard,
                QualifiedPositions = qualification.QualifiedPositions,
                QualifiedMaterials = qualification.QualifiedMaterials,
                ThicknessRange = qualification.ThicknessRange,
                DiameterRange = qualification.DiameterRange,
                IssueDate = qualification.IssueDate,
                ExpirationDate = qualification.ExpirationDate,
                Status = GetQualificationStatus(qualification.ExpirationDate),
                AiPreValidated = qualification.PreValidatedByAI,
                AiConfidenceScore = qualification.AIConfidenceScore,
                CertifyingBody = qualification.CertifyingBody
            });
        }

        /// <summary>
        /// Pre-validate qualification document using Gemini OCR
        /// </summary>
        [HttpPost("pre-validate")]
        public async Task<ActionResult<AIPreValidationResultDto>> PreValidateDocument([FromBody] PreValidateQualificationRequest request)
        {
            try
            {
                var result = await _geminiService.PreValidateQualificationAsync(new QualificationPreValidationRequest
                {
                    DocumentBase64 = request.DocumentBase64,
                    DocumentMimeType = request.DocumentMimeType,
                    QualificationType = request.QualificationType,
                    ExpectedStandard = request.ExpectedStandard
                });

                return Ok(new AIPreValidationResultDto
                {
                    Success = result.Success,
                    ConfidenceScore = result.ConfidenceScore,
                    ExtractedData = result.ExtractedData != null ? new ExtractedQualificationDataDto
                    {
                        QualificationNumber = result.ExtractedData.QualificationNumber,
                        HolderName = result.ExtractedData.HolderName,
                        WeldingProcess = result.ExtractedData.WeldingProcess,
                        CertificationLevel = result.ExtractedData.CertificationLevel,
                        QualifiedMaterials = result.ExtractedData.QualifiedMaterials,
                        ThicknessRange = result.ExtractedData.ThicknessRange,
                        DiameterRange = result.ExtractedData.DiameterRange,
                        QualifiedPositions = result.ExtractedData.QualifiedPositions,
                        QualificationStandard = result.ExtractedData.QualificationStandard,
                        CertifyingBody = result.ExtractedData.CertifyingBody,
                        IssueDate = result.ExtractedData.IssueDate,
                        ExpirationDate = result.ExtractedData.ExpirationDate
                    } : null,
                    Warnings = result.Warnings,
                    ValidationIssues = result.ValidationIssues,
                    AIModelVersion = result.AIModelVersion,
                    Error = result.Error
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error pre-validating qualification document");
                return Ok(new AIPreValidationResultDto
                {
                    Success = false,
                    Error = "Erreur lors de la pré-validation du document"
                });
            }
        }

        /// <summary>
        /// Create a new qualification (after AI pre-validation)
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<WelderQualificationDto>> CreateQualification([FromBody] CreateNuclearQualificationRequest request)
        {
            var customerId = GetCustomerId();
            if (customerId == null)
                return Forbid();

            // Verify welder exists
            var welder = await _context.Users.FindAsync(request.UserId);
            if (welder == null)
                return BadRequest("Soudeur non trouvé");

            var qualification = new WelderQualification
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId.Value,
                UserId = request.UserId,
                QualificationNumber = request.QualificationNumber,
                WeldingProcess = request.WeldingProcess,
                QualificationStandard = request.QualificationStandard,
                QualifiedPositions = request.QualifiedPositions,
                QualifiedMaterials = request.QualifiedMaterials,
                ThicknessRange = request.ThicknessRange,
                DiameterRange = request.DiameterRange,
                IssueDate = request.IssueDate,
                ExpirationDate = request.ExpirationDate,
                CertifyingBody = request.CertifyingBody,
                PreValidatedByAI = request.PreValidatedByAI,
                AIConfidenceScore = request.AIConfidenceScore,
                CreatedAt = DateTime.UtcNow
            };

            _context.WelderQualifications.Add(qualification);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Qualification {Number} créée pour soudeur {UserId}",
                qualification.QualificationNumber, qualification.UserId);

            return CreatedAtAction(nameof(GetQualification), new { id = qualification.Id }, new WelderQualificationDto
            {
                Id = qualification.Id,
                QualificationNumber = qualification.QualificationNumber,
                WelderId = qualification.UserId,
                WeldingProcess = qualification.WeldingProcess,
                QualificationStandard = qualification.QualificationStandard,
                QualifiedPositions = qualification.QualifiedPositions,
                IssueDate = qualification.IssueDate,
                ExpirationDate = qualification.ExpirationDate,
                Status = GetQualificationStatus(qualification.ExpirationDate),
                AiPreValidated = qualification.PreValidatedByAI,
                AiConfidenceScore = qualification.AIConfidenceScore
            });
        }

        /// <summary>
        /// Get dashboard statistics for qualifications
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<ActionResult<QualificationDashboardDto>> GetDashboard()
        {
            var customerId = GetCustomerId();
            if (customerId == null)
                return Forbid();

            var now = DateTime.UtcNow;
            var thirtyDaysFromNow = now.AddDays(30);

            var qualifications = await _context.WelderQualifications
                .Where(q => q.CustomerId == customerId && q.IsActive)
                .ToListAsync();

            return Ok(new QualificationDashboardDto
            {
                TotalQualifications = qualifications.Count,
                ValidQualifications = qualifications.Count(q => q.ExpirationDate > now),
                ExpiringWithin30Days = qualifications.Count(q => q.ExpirationDate <= thirtyDaysFromNow && q.ExpirationDate > now),
                ExpiredQualifications = qualifications.Count(q => q.ExpirationDate <= now),
                AIPreValidatedCount = qualifications.Count(q => q.PreValidatedByAI),
                AverageAIConfidence = qualifications.Where(q => q.PreValidatedByAI && q.AIConfidenceScore.HasValue)
                    .Select(q => q.AIConfidenceScore!.Value).DefaultIfEmpty(0).Average()
            });
        }

        private static string GetQualificationStatus(DateTime expirationDate)
        {
            var now = DateTime.UtcNow;
            if (expirationDate <= now)
                return "EXPIRED";
            if (expirationDate <= now.AddDays(30))
                return "EXPIRING_SOON";
            return "VALID";
        }
    }

    // Request DTOs
    public class PreValidateQualificationRequest
    {
        public string DocumentBase64 { get; set; } = string.Empty;
        public string DocumentMimeType { get; set; } = string.Empty;
        public string QualificationType { get; set; } = string.Empty;
        public string? ExpectedStandard { get; set; }
    }

    public class CreateNuclearQualificationRequest
    {
        public Guid UserId { get; set; }
        public string QualificationNumber { get; set; } = string.Empty;
        public string WeldingProcess { get; set; } = string.Empty;
        public string QualificationStandard { get; set; } = string.Empty;
        public string QualifiedPositions { get; set; } = string.Empty;
        public string? QualifiedMaterials { get; set; }
        public string? ThicknessRange { get; set; }
        public string? DiameterRange { get; set; }
        public DateTime IssueDate { get; set; }
        public DateTime ExpirationDate { get; set; }
        public string? CertifyingBody { get; set; }
        public bool PreValidatedByAI { get; set; }
        public decimal? AIConfidenceScore { get; set; }
    }
}
