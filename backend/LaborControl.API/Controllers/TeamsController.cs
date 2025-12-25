using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using LaborControl.API.Data;
using LaborControl.API.DTOs;
using LaborControl.API.Models;

namespace LaborControl.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class TeamsController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TeamsController> _logger;

        public TeamsController(ApplicationDbContext context, ILogger<TeamsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Récupère toutes les équipes du client connecté (Services et Équipes)
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<List<TeamResponse>>> GetTeams()
        {
            var customerIdClaim = User.FindFirst("CustomerId");
            if (customerIdClaim == null || !Guid.TryParse(customerIdClaim.Value, out var customerId))
            {
                return BadRequest(new { message = "CustomerId introuvable dans le token" });
            }

            var teams = await _context.Teams
                .Include(t => t.Supervisor)
                .Include(t => t.Members)
                .Include(t => t.SubTeams)
                .Include(t => t.Sector)
                .Where(t => t.CustomerId == customerId)
                .OrderBy(t => t.Level)
                .ThenBy(t => t.DisplayOrder)
                .ThenBy(t => t.Name)
                .Select(t => new TeamResponse
                {
                    Id = t.Id,
                    CustomerId = t.CustomerId,
                    Name = t.Name,
                    Code = t.Code,
                    SectorId = t.SectorId,
                    SectorName = t.Sector != null ? t.Sector.Name : null,
                    Description = t.Description,
                    SupervisorId = t.SupervisorId,
                    SupervisorName = t.Supervisor != null ? $"{t.Supervisor.Prenom} {t.Supervisor.Nom}" : null,
                    IsActive = t.IsActive,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    MembersCount = t.Members.Count,
                    ParentTeamId = t.ParentTeamId,
                    Level = t.Level,
                    DisplayOrder = t.DisplayOrder,
                    SubTeamsCount = t.SubTeams.Count
                })
                .ToListAsync();

            return Ok(teams);
        }

        /// <summary>
        /// Récupère une équipe par son ID avec ses membres et sous-équipes
        /// </summary>
        [HttpGet("{id}")]
        public async Task<ActionResult<TeamDetailResponse>> GetTeam(Guid id)
        {
            var customerIdClaim = User.FindFirst("CustomerId");
            if (customerIdClaim == null || !Guid.TryParse(customerIdClaim.Value, out var customerId))
            {
                return BadRequest(new { message = "CustomerId introuvable dans le token" });
            }

            var team = await _context.Teams
                .Include(t => t.Supervisor)
                .Include(t => t.Members)
                .Include(t => t.Sector)
                .Include(t => t.SubTeams)
                    .ThenInclude(st => st.Supervisor)
                .Include(t => t.SubTeams)
                    .ThenInclude(st => st.Members)
                .Include(t => t.SubTeams)
                    .ThenInclude(st => st.Sector)
                .Where(t => t.Id == id && t.CustomerId == customerId)
                .Select(t => new TeamDetailResponse
                {
                    Id = t.Id,
                    CustomerId = t.CustomerId,
                    Name = t.Name,
                    Code = t.Code,
                    SectorId = t.SectorId,
                    SectorName = t.Sector != null ? t.Sector.Name : null,
                    Description = t.Description,
                    SupervisorId = t.SupervisorId,
                    SupervisorName = t.Supervisor != null ? $"{t.Supervisor.Prenom} {t.Supervisor.Nom}" : null,
                    IsActive = t.IsActive,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    Members = t.Members.Select(m => new TeamMemberResponse
                    {
                        Id = m.Id,
                        Nom = m.Nom,
                        Prenom = m.Prenom,
                        Email = m.Email,
                        Niveau = m.Niveau,
                        Role = m.Role,
                        IsActive = m.IsActive
                    }).ToList(),
                    ParentTeamId = t.ParentTeamId,
                    Level = t.Level,
                    DisplayOrder = t.DisplayOrder,
                    SubTeams = t.SubTeams.Select(st => new TeamResponse
                    {
                        Id = st.Id,
                        CustomerId = st.CustomerId,
                        Name = st.Name,
                        Code = st.Code,
                        SectorId = st.SectorId,
                        SectorName = st.Sector != null ? st.Sector.Name : null,
                        Description = st.Description,
                        SupervisorId = st.SupervisorId,
                        SupervisorName = st.Supervisor != null ? $"{st.Supervisor.Prenom} {st.Supervisor.Nom}" : null,
                        IsActive = st.IsActive,
                        CreatedAt = st.CreatedAt,
                        UpdatedAt = st.UpdatedAt,
                        MembersCount = st.Members.Count,
                        ParentTeamId = st.ParentTeamId,
                        Level = st.Level,
                        DisplayOrder = st.DisplayOrder,
                        SubTeamsCount = 0
                    }).ToList()
                })
                .FirstOrDefaultAsync();

            if (team == null)
            {
                return NotFound(new { message = "Équipe non trouvée" });
            }

            return Ok(team);
        }

        /// <summary>
        /// Crée une nouvelle équipe ou un nouveau service
        /// </summary>
        [HttpPost]
        public async Task<ActionResult<TeamResponse>> CreateTeam([FromBody] CreateTeamRequest request)
        {
            var customerIdClaim = User.FindFirst("CustomerId");
            if (customerIdClaim == null || !Guid.TryParse(customerIdClaim.Value, out var customerId))
            {
                return BadRequest(new { message = "CustomerId introuvable dans le token" });
            }

            // Vérifier que le service parent existe (si spécifié)
            if (request.ParentTeamId.HasValue)
            {
                var parentTeamExists = await _context.Teams
                    .AnyAsync(t => t.Id == request.ParentTeamId.Value && t.CustomerId == customerId);
                if (!parentTeamExists)
                {
                    return BadRequest(new { message = "Service parent non trouvé ou n'appartient pas à votre entreprise" });
                }
            }

            // Vérifier que le superviseur appartient au client (si spécifié)
            if (request.SupervisorId.HasValue)
            {
                var supervisorExists = await _context.Users
                    .AnyAsync(u => u.Id == request.SupervisorId.Value && u.CustomerId == customerId);
                if (!supervisorExists)
                {
                    return BadRequest(new { message = "Superviseur non trouvé ou n'appartient pas à votre entreprise" });
                }
            }

            var team = new Team
            {
                Id = Guid.NewGuid(),
                CustomerId = customerId,
                Name = request.Name,
                Code = request.Code,
                SectorId = request.SectorId,
                Description = request.Description,
                SupervisorId = request.SupervisorId,
                WorkShiftSystem = request.WorkShiftSystem,
                RotationFrequency = request.RotationFrequency,
                WorksSaturday = request.WorksSaturday,
                WorksSunday = request.WorksSunday,
                HasOnCallDuty = request.HasOnCallDuty,
                ParentTeamId = request.ParentTeamId,
                Level = request.Level,
                DisplayOrder = request.DisplayOrder,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            _context.Teams.Add(team);
            await _context.SaveChangesAsync();

            // Recharger avec les relations
            var createdTeam = await _context.Teams
                .Include(t => t.Supervisor)
                .Include(t => t.Members)
                .Include(t => t.SubTeams)
                .Include(t => t.Sector)
                .Where(t => t.Id == team.Id)
                .Select(t => new TeamResponse
                {
                    Id = t.Id,
                    CustomerId = t.CustomerId,
                    Name = t.Name,
                    Code = t.Code,
                    SectorId = t.SectorId,
                    SectorName = t.Sector != null ? t.Sector.Name : null,
                    Description = t.Description,
                    SupervisorId = t.SupervisorId,
                    SupervisorName = t.Supervisor != null ? $"{t.Supervisor.Prenom} {t.Supervisor.Nom}" : null,
                    IsActive = t.IsActive,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    MembersCount = t.Members.Count,
                    ParentTeamId = t.ParentTeamId,
                    Level = t.Level,
                    DisplayOrder = t.DisplayOrder,
                    SubTeamsCount = t.SubTeams.Count
                })
                .FirstAsync();

            return CreatedAtAction(nameof(GetTeam), new { id = team.Id }, createdTeam);
        }

        /// <summary>
        /// Met à jour une équipe ou un service existant
        /// </summary>
        [HttpPut("{id}")]
        public async Task<ActionResult<TeamResponse>> UpdateTeam(Guid id, [FromBody] UpdateTeamRequest request)
        {
            var customerIdClaim = User.FindFirst("CustomerId");
            if (customerIdClaim == null || !Guid.TryParse(customerIdClaim.Value, out var customerId))
            {
                return BadRequest(new { message = "CustomerId introuvable dans le token" });
            }

            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.Id == id && t.CustomerId == customerId);

            if (team == null)
            {
                return NotFound(new { message = "Équipe non trouvée" });
            }

            // Vérifier que le service parent existe (si spécifié et différent)
            if (request.ParentTeamId.HasValue && request.ParentTeamId != team.ParentTeamId)
            {
                // Ne pas permettre de définir un parent qui est soi-même
                if (request.ParentTeamId.Value == id)
                {
                    return BadRequest(new { message = "Une équipe ne peut pas être son propre parent" });
                }

                var parentTeamExists = await _context.Teams
                    .AnyAsync(t => t.Id == request.ParentTeamId.Value && t.CustomerId == customerId);
                if (!parentTeamExists)
                {
                    return BadRequest(new { message = "Service parent non trouvé ou n'appartient pas à votre entreprise" });
                }
            }

            // Vérifier que le superviseur appartient au client (si spécifié)
            if (request.SupervisorId.HasValue)
            {
                var supervisorExists = await _context.Users
                    .AnyAsync(u => u.Id == request.SupervisorId.Value && u.CustomerId == customerId);
                if (!supervisorExists)
                {
                    return BadRequest(new { message = "Superviseur non trouvé ou n'appartient pas à votre entreprise" });
                }
            }

            team.Name = request.Name;
            team.Code = request.Code;
            team.SectorId = request.SectorId;
            team.Description = request.Description;
            team.SupervisorId = request.SupervisorId;
            team.IsActive = request.IsActive;
            team.ParentTeamId = request.ParentTeamId;
            team.Level = request.Level;
            team.DisplayOrder = request.DisplayOrder;
            team.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // Recharger avec les relations
            var updatedTeam = await _context.Teams
                .Include(t => t.Supervisor)
                .Include(t => t.Members)
                .Include(t => t.SubTeams)
                .Include(t => t.Sector)
                .Where(t => t.Id == team.Id)
                .Select(t => new TeamResponse
                {
                    Id = t.Id,
                    CustomerId = t.CustomerId,
                    Name = t.Name,
                    Code = t.Code,
                    SectorId = t.SectorId,
                    SectorName = t.Sector != null ? t.Sector.Name : null,
                    Description = t.Description,
                    SupervisorId = t.SupervisorId,
                    SupervisorName = t.Supervisor != null ? $"{t.Supervisor.Prenom} {t.Supervisor.Nom}" : null,
                    IsActive = t.IsActive,
                    CreatedAt = t.CreatedAt,
                    UpdatedAt = t.UpdatedAt,
                    MembersCount = t.Members.Count,
                    ParentTeamId = t.ParentTeamId,
                    Level = t.Level,
                    DisplayOrder = t.DisplayOrder,
                    SubTeamsCount = t.SubTeams.Count
                })
                .FirstAsync();

            return Ok(updatedTeam);
        }

        /// <summary>
        /// Supprime une équipe ou un service
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteTeam(Guid id)
        {
            var customerIdClaim = User.FindFirst("CustomerId");
            if (customerIdClaim == null || !Guid.TryParse(customerIdClaim.Value, out var customerId))
            {
                return BadRequest(new { message = "CustomerId introuvable dans le token" });
            }

            var team = await _context.Teams
                .Include(t => t.Members)
                .Include(t => t.SubTeams)
                .FirstOrDefaultAsync(t => t.Id == id && t.CustomerId == customerId);

            if (team == null)
            {
                return NotFound(new { message = "Équipe non trouvée" });
            }

            // Vérifier si le service/équipe a des sous-équipes
            if (team.SubTeams.Any())
            {
                return BadRequest(new { message = "Impossible de supprimer un service qui contient des équipes. Veuillez d'abord supprimer ou déplacer les équipes." });
            }

            // Retirer tous les membres de l'équipe avant de la supprimer
            foreach (var member in team.Members)
            {
                member.TeamId = null;
            }

            _context.Teams.Remove(team);
            await _context.SaveChangesAsync();

            return Ok(new { message = team.Level == 0 ? "Service supprimé avec succès" : "Équipe supprimée avec succès" });
        }

        /// <summary>
        /// Affecte un utilisateur à une équipe
        /// </summary>
        [HttpPost("assign-user")]
        public async Task<ActionResult> AssignUserToTeam([FromBody] AssignUserToTeamRequest request)
        {
            var customerIdClaim = User.FindFirst("CustomerId");
            if (customerIdClaim == null || !Guid.TryParse(customerIdClaim.Value, out var customerId))
            {
                return BadRequest(new { message = "CustomerId introuvable dans le token" });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId && u.CustomerId == customerId);

            if (user == null)
            {
                return NotFound(new { message = "Utilisateur non trouvé" });
            }

            var team = await _context.Teams
                .FirstOrDefaultAsync(t => t.Id == request.TeamId && t.CustomerId == customerId);

            if (team == null)
            {
                return NotFound(new { message = "Équipe non trouvée" });
            }

            user.TeamId = request.TeamId;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Utilisateur affecté à l'équipe avec succès" });
        }

        /// <summary>
        /// Retire un utilisateur d'une équipe
        /// </summary>
        [HttpPost("remove-user")]
        public async Task<ActionResult> RemoveUserFromTeam([FromBody] RemoveUserFromTeamRequest request)
        {
            var customerIdClaim = User.FindFirst("CustomerId");
            if (customerIdClaim == null || !Guid.TryParse(customerIdClaim.Value, out var customerId))
            {
                return BadRequest(new { message = "CustomerId introuvable dans le token" });
            }

            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.UserId && u.CustomerId == customerId);

            if (user == null)
            {
                return NotFound(new { message = "Utilisateur non trouvé" });
            }

            user.TeamId = null;
            await _context.SaveChangesAsync();

            return Ok(new { message = "Utilisateur retiré de l'équipe avec succès" });
        }
    }
}
