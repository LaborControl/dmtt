using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    public class Task
    {
        public Guid Id { get; set; }
        
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;
        
        public string Description { get; set; } = string.Empty;
        
        public Guid UserId { get; set; }
        public User? User { get; set; }
        
        public Guid PointId { get; set; }
        public ControlPoint? ControlPoint { get; set; }
        
        public DateTime ScheduledDate { get; set; }
        
        [MaxLength(20)]
        public string Recurrence { get; set; } = "none"; // none, daily, weekly, monthly
        
        [MaxLength(20)]
        public string Status { get; set; } = "pending"; // pending, completed, cancelled
        
        public Guid CustomerId { get; set; }
        public Customer? Customer { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
