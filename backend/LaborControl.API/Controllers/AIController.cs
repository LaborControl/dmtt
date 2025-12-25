using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using LaborControl.API.Services.AI;
using System.Security.Claims;

namespace LaborControl.API.Controllers
{
    /// <summary>
    /// AI Controller for DMOS and NDT Program generation
    /// Uses Claude AI for procedure generation
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class AIController : ControllerBase
    {
        private readonly IAIService _aiService;
        private readonly ILogger<AIController> _logger;

        public AIController(
            IAIService aiService,
            ILogger<AIController> logger)
        {
            _aiService = aiService;
            _logger = logger;
        }

        private Guid GetUserId() => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        /// <summary>
        /// Generate DMOS (Welding Procedure Specification) using Claude AI
        /// </summary>
        [HttpPost("generate-dmos")]
        public async Task<ActionResult<AIGenerationResult>> GenerateDMOS([FromBody] DMOSGenerationRequest request)
        {
            try
            {
                _logger.LogInformation("User {UserId} requesting DMOS generation for process {Process}",
                    GetUserId(), request.WeldingProcess);

                var result = await _aiService.GenerateDMOSAsync(request);

                if (!result.Success)
                {
                    _logger.LogWarning("DMOS generation failed: {Error}", result.Error);
                }
                else
                {
                    _logger.LogInformation("DMOS generated successfully. Tokens used: {Tokens}", result.TokensUsed);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating DMOS");
                return Ok(new AIGenerationResult
                {
                    Success = false,
                    Error = "Erreur lors de la génération du DMOS"
                });
            }
        }

        /// <summary>
        /// Generate NDT Program using Claude AI
        /// </summary>
        [HttpPost("generate-ndt-program")]
        public async Task<ActionResult<AIGenerationResult>> GenerateNDTProgram([FromBody] NDTProgramGenerationRequest request)
        {
            try
            {
                _logger.LogInformation("User {UserId} requesting NDT program generation for asset {Asset}",
                    GetUserId(), request.AssetName);

                var result = await _aiService.GenerateNDTProgramAsync(request);

                if (!result.Success)
                {
                    _logger.LogWarning("NDT program generation failed: {Error}", result.Error);
                }
                else
                {
                    _logger.LogInformation("NDT program generated successfully. Tokens used: {Tokens}", result.TokensUsed);
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating NDT program");
                return Ok(new AIGenerationResult
                {
                    Success = false,
                    Error = "Erreur lors de la génération du programme CND"
                });
            }
        }

        /// <summary>
        /// Adapt NDT Program based on detected defects
        /// </summary>
        [HttpPost("adapt-ndt-program")]
        public async Task<ActionResult<AIAdaptationResult>> AdaptNDTProgram([FromBody] NDTProgramAdaptationRequest request)
        {
            try
            {
                _logger.LogInformation("User {UserId} requesting NDT program adaptation for {DefectCount} defects",
                    GetUserId(), request.DefectsFound.Count);

                var result = await _aiService.AdaptNDTProgramAsync(request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error adapting NDT program");
                return Ok(new AIAdaptationResult
                {
                    Success = false,
                    Error = "Erreur lors de l'adaptation du programme CND"
                });
            }
        }

        /// <summary>
        /// Get AI planning assistance for welding work
        /// </summary>
        [HttpPost("planning-assistance")]
        public async Task<ActionResult<AIPlanningResult>> GetPlanningAssistance([FromBody] PlanningAssistanceRequest request)
        {
            try
            {
                _logger.LogInformation("User {UserId} requesting planning assistance for {WeldCount} welds",
                    GetUserId(), request.TotalWelds);

                var result = await _aiService.GetPlanningAssistanceAsync(request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting planning assistance");
                return Ok(new AIPlanningResult
                {
                    Success = false,
                    Error = "Erreur lors de l'assistance à la planification"
                });
            }
        }

        /// <summary>
        /// Get AI recommendations for non-conformity resolution
        /// </summary>
        [HttpPost("nc-recommendation")]
        public async Task<ActionResult<AIRecommendationResult>> GetNCRecommendation([FromBody] NCRecommendationRequest request)
        {
            try
            {
                _logger.LogInformation("User {UserId} requesting NC recommendation for type {NCType}",
                    GetUserId(), request.NCType);

                var result = await _aiService.GetNCRecommendationAsync(request);

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting NC recommendation");
                return Ok(new AIRecommendationResult
                {
                    Success = false,
                    Error = "Erreur lors de la recommandation IA"
                });
            }
        }
    }
}
