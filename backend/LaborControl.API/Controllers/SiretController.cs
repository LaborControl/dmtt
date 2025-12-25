using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LaborControl.API.Controllers;
using LaborControl.API.Services;
using System.Text.Json;

namespace LaborControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SiretController : ControllerBase
    {
        private readonly ILogger<SiretController> _logger;
        private readonly SiretVerificationService _siretVerificationService;

        public SiretController(
            ILogger<SiretController> logger,
            SiretVerificationService siretVerificationService)
        {
            _logger = logger;
            _siretVerificationService = siretVerificationService;
        }

        /// <summary>
        /// GET /api/siret/validate/{siret}
        /// Valide un SIRET via l'API INSEE (endpoint public pour compatibilité)
        /// </summary>
        [HttpGet("validate/{siret}")]
        [AllowAnonymous]
        public async Task<ActionResult<object>> ValidateSiret(string siret)
        {
            try
            {
                // Nettoyer le SIRET
                siret = siret?.Replace(" ", "").Trim() ?? "";

                if (string.IsNullOrWhiteSpace(siret) || siret.Length != 14)
                {
                    return BadRequest(new { isValid = false, errorMessage = "SIRET invalide (14 chiffres requis)" });
                }

                // Utiliser le service de vérification qui inclut le token INSEE
                var result = await _siretVerificationService.VerifySiretAsync(siret);

                return Ok(new
                {
                    isValid = result.IsValid,
                    isActive = result.IsActive,
                    companyName = result.CompanyName,
                    address = result.Address,
                    postalCode = result.PostalCode,
                    city = result.City,
                    activityCode = result.ActivityCode,
                    errorMessage = result.ErrorMessage
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erreur ValidateSiret");
                return StatusCode(500, new { isValid = false, errorMessage = "Erreur lors de la vérification du SIRET" });
            }
        }
    }
}
