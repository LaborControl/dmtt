using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    /// <summary>
    /// Table de liaison Many-to-Many entre Team et Site
    /// Une équipe peut intervenir sur plusieurs sites
    /// Un site peut avoir plusieurs équipes
    /// </summary>
    public class TeamSite
    {
        [Required]
        public Guid TeamId { get; set; }
        public Team Team { get; set; } = null!;

        [Required]
        public Guid SiteId { get; set; }
        public Site Site { get; set; } = null!;

        /// <summary>
        /// Date d'affectation de l'équipe à ce site
        /// </summary>
        public DateTime AssignedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Indique si l'affectation est active
        /// </summary>
        public bool IsActive { get; set; } = true;
    }
}
