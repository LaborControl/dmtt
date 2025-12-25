using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    public class TaskDeviation
    {
        public Guid Id { get; set; }
        
        [Required]
        public Guid TaskExecutionId { get; set; }
        public TaskExecution? TaskExecution { get; set; }
        
        [Required]
        public Guid CustomerId { get; set; }
        public Customer? Customer { get; set; }
        
        [Required]
        [MaxLength(20)]
        public string ExpectedQualification { get; set; } = string.Empty;
        
        [Required]
        [MaxLength(20)]
        public string ActualQualification { get; set; } = string.Empty;
        
        [Required]
        public Guid PerformedByUserId { get; set; }
        public User? PerformedByUser { get; set; }
        
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        public bool WasApproved { get; set; } = false;
        
        public Guid? ApprovedByUserId { get; set; }
        public User? ApprovedByUser { get; set; }
        
        [MaxLength(500)]
        public string? JustificationComment { get; set; }
        
        public bool IsReported { get; set; } = false;
    }
}