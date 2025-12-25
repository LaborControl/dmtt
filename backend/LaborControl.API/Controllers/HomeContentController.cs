using LaborControl.API.Data;
using LaborControl.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace LaborControl.API.Controllers
{
    /// <summary>
    /// Contrôleur pour la gestion du contenu éditable de la page d'accueil.
    /// </summary>
    [ApiController]
    [Route("api/home-content")]
    [Authorize]
    public class HomeContentController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<HomeContentController> _logger;

        public HomeContentController(ApplicationDbContext context, ILogger<HomeContentController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Récupère le contenu publié de la page d'accueil (accessible à tous les utilisateurs authentifiés).
        /// </summary>
        /// <returns>Contenu publié ou 404 si aucun contenu publié</returns>
        [HttpGet("published")]
        [AllowAnonymous]
        public async Task<ActionResult<HomeContentDto>> GetPublishedContent()
        {
            try
            {
                var content = await _context.HomeContents
                    .Where(h => h.IsPublished)
                    .OrderByDescending(h => h.PublishedAt)
                    .FirstOrDefaultAsync();

                if (content == null)
                {
                    _logger.LogWarning("Aucun contenu publié trouvé pour la page d'accueil");
                    return NotFound(new { message = "Aucun contenu publié" });
                }

                return Ok(MapToDto(content));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du contenu publié");
                return StatusCode(500, new { error = "Erreur serveur" });
            }
        }

        /// <summary>
        /// Récupère le brouillon du contenu.
        /// </summary>
        /// <returns>Brouillon ou 404 si aucun brouillon</returns>
        [HttpGet("draft")]
        [Authorize]
        public async Task<ActionResult<HomeContentDto>> GetDraftContent()
        {
            try
            {
                var content = await _context.HomeContents
                    .Where(h => !h.IsPublished)
                    .OrderByDescending(h => h.UpdatedAt)
                    .FirstOrDefaultAsync();

                if (content == null)
                {
                    _logger.LogWarning("Aucun brouillon trouvé pour la page d'accueil");
                    return NotFound(new { message = "Aucun brouillon" });
                }

                return Ok(MapToDto(content));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du brouillon");
                return StatusCode(500, new { error = "Erreur serveur" });
            }
        }

        /// <summary>
        /// Récupère le contenu par ID.
        /// </summary>
        /// <param name="id">ID du contenu</param>
        /// <returns>Contenu ou 404</returns>
        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<HomeContentDto>> GetContentById(Guid id)
        {
            try
            {
                var content = await _context.HomeContents.FindAsync(id);

                if (content == null)
                {
                    return NotFound(new { message = "Contenu non trouvé" });
                }

                return Ok(MapToDto(content));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération du contenu");
                return StatusCode(500, new { error = "Erreur serveur" });
            }
        }

        /// <summary>
        /// Crée un nouveau brouillon de contenu.
        /// </summary>
        /// <param name="request">Données du contenu</param>
        /// <returns>Contenu créé</returns>
        [HttpPost]
        [Authorize]
        public async Task<ActionResult<HomeContentDto>> CreateContent([FromBody] CreateHomeContentRequest request)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(request.Content))
                {
                    return BadRequest(new { error = "Le contenu ne peut pas être vide" });
                }

                var homeContent = new HomeContent
                {
                    Id = Guid.NewGuid(),
                    Content = request.Content,
                    IsPublished = false,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow,
                    Version = 1
                };

                _context.HomeContents.Add(homeContent);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Nouveau contenu créé avec ID: {ContentId}", homeContent.Id);

                return CreatedAtAction(nameof(GetContentById), new { id = homeContent.Id }, MapToDto(homeContent));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la création du contenu");
                return StatusCode(500, new { error = "Erreur serveur" });
            }
        }

        /// <summary>
        /// Met à jour le contenu.
        /// Incrémente automatiquement le numéro de version.
        /// </summary>
        /// <param name="id">ID du contenu</param>
        /// <param name="request">Données mises à jour</param>
        /// <returns>Contenu mis à jour</returns>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<ActionResult<HomeContentDto>> UpdateContent(Guid id, [FromBody] UpdateHomeContentRequest request)
        {
            try
            {
                var content = await _context.HomeContents.FindAsync(id);

                if (content == null)
                {
                    return NotFound(new { message = "Contenu non trouvé" });
                }

                if (string.IsNullOrWhiteSpace(request.Content))
                {
                    return BadRequest(new { error = "Le contenu ne peut pas être vide" });
                }

                content.Content = request.Content;
                content.UpdatedAt = DateTime.UtcNow;
                content.Version++;

                _context.HomeContents.Update(content);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Contenu mis à jour: {ContentId}, Version: {Version}", id, content.Version);

                return Ok(MapToDto(content));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la mise à jour du contenu");
                return StatusCode(500, new { error = "Erreur serveur" });
            }
        }

        /// <summary>
        /// Publie le contenu.
        /// Dépublie automatiquement le contenu précédemment publié.
        /// </summary>
        /// <param name="id">ID du contenu à publier</param>
        /// <returns>Contenu publié</returns>
        [HttpPost("{id}/publish")]
        [Authorize]
        public async Task<ActionResult<HomeContentDto>> PublishContent(Guid id)
        {
            try
            {
                var content = await _context.HomeContents.FindAsync(id);

                if (content == null)
                {
                    return NotFound(new { message = "Contenu non trouvé" });
                }

                // Dépublier le contenu précédemment publié
                var previousPublished = await _context.HomeContents
                    .Where(h => h.IsPublished && h.Id != id)
                    .ToListAsync();

                foreach (var item in previousPublished)
                {
                    item.IsPublished = false;
                    item.UpdatedAt = DateTime.UtcNow;
                }

                // Publier le nouveau contenu
                content.IsPublished = true;
                content.PublishedAt = DateTime.UtcNow;
                content.UpdatedAt = DateTime.UtcNow;

                _context.HomeContents.UpdateRange(previousPublished);
                _context.HomeContents.Update(content);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Contenu publié: {ContentId}", id);

                return Ok(MapToDto(content));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la publication du contenu");
                return StatusCode(500, new { error = "Erreur serveur" });
            }
        }

        /// <summary>
        /// Dépublie le contenu.
        /// </summary>
        /// <param name="id">ID du contenu à dépublier</param>
        /// <returns>Contenu dépublié</returns>
        [HttpPost("{id}/unpublish")]
        [Authorize]
        public async Task<ActionResult<HomeContentDto>> UnpublishContent(Guid id)
        {
            try
            {
                var content = await _context.HomeContents.FindAsync(id);

                if (content == null)
                {
                    return NotFound(new { message = "Contenu non trouvé" });
                }

                content.IsPublished = false;
                content.UpdatedAt = DateTime.UtcNow;

                _context.HomeContents.Update(content);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Contenu dépublié: {ContentId}", id);

                return Ok(MapToDto(content));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la dépublication du contenu");
                return StatusCode(500, new { error = "Erreur serveur" });
            }
        }

        /// <summary>
        /// Supprime le contenu.
        /// </summary>
        /// <param name="id">ID du contenu à supprimer</param>
        /// <returns>204 No Content</returns>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteContent(Guid id)
        {
            try
            {
                var content = await _context.HomeContents.FindAsync(id);

                if (content == null)
                {
                    return NotFound(new { message = "Contenu non trouvé" });
                }

                _context.HomeContents.Remove(content);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Contenu supprimé: {ContentId}", id);

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la suppression du contenu");
                return StatusCode(500, new { error = "Erreur serveur" });
            }
        }

        /// <summary>
        /// Récupère l'historique des versions.
        /// </summary>
        /// <returns>Liste des versions</returns>
        [HttpGet("history/versions")]
        [Authorize]
        public async Task<ActionResult<List<HomeContentDto>>> GetVersionHistory()
        {
            try
            {
                var contents = await _context.HomeContents
                    .OrderByDescending(h => h.UpdatedAt)
                    .ToListAsync();

                return Ok(contents.Select(MapToDto).ToList());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erreur lors de la récupération de l'historique");
                return StatusCode(500, new { error = "Erreur serveur" });
            }
        }

        /// <summary>
        /// Mappe un modèle HomeContent vers un DTO.
        /// </summary>
        private static HomeContentDto MapToDto(HomeContent content)
        {
            return new HomeContentDto
            {
                Id = content.Id,
                Content = content.Content,
                IsPublished = content.IsPublished,
                PublishedAt = content.PublishedAt,
                UpdatedAt = content.UpdatedAt,
                CreatedAt = content.CreatedAt,
                Version = content.Version
            };
        }
    }

    /// <summary>
    /// DTO pour la création de contenu.
    /// </summary>
    public class CreateHomeContentRequest
    {
        /// <summary>
        /// Contenu au format JSON.
        /// </summary>
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO pour la mise à jour de contenu.
    /// </summary>
    public class UpdateHomeContentRequest
    {
        /// <summary>
        /// Contenu au format JSON.
        /// </summary>
        public string Content { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO pour la réponse du contenu.
    /// </summary>
    public class HomeContentDto
    {
        /// <summary>
        /// Identifiant unique.
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Contenu au format JSON.
        /// </summary>
        public string Content { get; set; } = string.Empty;

        /// <summary>
        /// Indique si le contenu est publié.
        /// </summary>
        public bool IsPublished { get; set; }

        /// <summary>
        /// Date de publication.
        /// </summary>
        public DateTime? PublishedAt { get; set; }

        /// <summary>
        /// Date de dernière modification.
        /// </summary>
        public DateTime UpdatedAt { get; set; }

        /// <summary>
        /// Date de création.
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Numéro de version.
        /// </summary>
        public int Version { get; set; }
    }
}
