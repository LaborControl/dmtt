using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LaborControl.API.Services;

namespace LaborControl.API.Controllers
{
    /// <summary>
    /// Contrôleur pour l'intégration avec France Compétences (RNCP et RS)
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/staff/france-competences")]
    public class FranceCompetencesController : ControllerBase
    {
        private readonly IFranceCompetencesService _franceCompetencesService;
        private readonly ILogger<FranceCompetencesController> _logger;

        public FranceCompetencesController(
            IFranceCompetencesService franceCompetencesService,
            ILogger<FranceCompetencesController> logger)
        {
            _franceCompetencesService = franceCompetencesService;
            _logger = logger;
        }

        /// <summary>
        /// Recherche dans le Répertoire National des Certifications Professionnelles (RNCP)
        /// </summary>
        [HttpGet("rncp/search")]
        public async Task<ActionResult<FranceCompetencesSearchResult>> SearchRncp(
            [FromQuery] string query,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest(new { message = "Le paramètre 'query' est requis" });
                }

                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var result = await _franceCompetencesService.SearchRncpAsync(query, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recherche RNCP");
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Recherche dans le Répertoire Spécifique (RS)
        /// </summary>
        [HttpGet("rs/search")]
        public async Task<ActionResult<FranceCompetencesSearchResult>> SearchRs(
            [FromQuery] string query,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(query))
                {
                    return BadRequest(new { message = "Le paramètre 'query' est requis" });
                }

                if (page < 1) page = 1;
                if (pageSize < 1 || pageSize > 100) pageSize = 20;

                var result = await _franceCompetencesService.SearchRsAsync(query, page, pageSize);
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la recherche RS");
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Récupère les détails d'une certification RNCP
        /// </summary>
        [HttpGet("rncp/{rncpCode}")]
        public async Task<ActionResult<RncpCertificationInfo>> GetRncpDetails(string rncpCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rncpCode))
                {
                    return BadRequest(new { message = "Le code RNCP est requis" });
                }

                var details = await _franceCompetencesService.GetRncpDetailsAsync(rncpCode);

                if (details == null)
                {
                    return NotFound(new { message = $"Certification RNCP {rncpCode} introuvable" });
                }

                return Ok(details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des détails RNCP {Code}", rncpCode);
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }

        /// <summary>
        /// Récupère les détails d'une certification RS
        /// </summary>
        [HttpGet("rs/{rsCode}")]
        public async Task<ActionResult<RsCertificationInfo>> GetRsDetails(string rsCode)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(rsCode))
                {
                    return BadRequest(new { message = "Le code RS est requis" });
                }

                var details = await _franceCompetencesService.GetRsDetailsAsync(rsCode);

                if (details == null)
                {
                    return NotFound(new { message = $"Certification RS {rsCode} introuvable" });
                }

                return Ok(details);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération des détails RS {Code}", rsCode);
                return StatusCode(500, new { message = "Erreur serveur", error = ex.Message });
            }
        }
    }
}
