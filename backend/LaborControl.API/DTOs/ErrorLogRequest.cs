namespace LaborControl.API.DTOs
{
    public class ErrorLogRequest
    {
        public string Message { get; set; } = string.Empty;
        public string? StackTrace { get; set; }
        public string PageUrl { get; set; } = string.Empty;
        public string? AdditionalData { get; set; }
        public string? UserAgent { get; set; }
        public string? UserEmail { get; set; }
        public Guid? CustomerId { get; set; }
        public string AppType { get; set; } = "CLIENT"; // CLIENT ou STAFF
        public string Severity { get; set; } = "MEDIUM"; // LOW, MEDIUM, HIGH, CRITICAL
    }
}
