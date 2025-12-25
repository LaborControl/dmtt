namespace LaborControl.API.DTOs
{
    public class CreateUserRequest
    {
        public string? Email { get; set; }
        public string? Username { get; set; }
        public string Password { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string Nom { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string Role { get; set; } = "TECHNICIAN";
        public Guid SectorId { get; set; }
        public Guid IndustryId { get; set; }
        public Guid CustomerId { get; set; }
        public Guid? SiteId { get; set; }
        public Guid? TeamId { get; set; }
        public List<UserQualificationRequest> Qualifications { get; set; } = new();
    }

    public class UserQualificationRequest
    {
        public Guid QualificationId { get; set; }
        public DateTime ObtainedDate { get; set; }
    }

    public class ChangePasswordRequest
    {
        public Guid UserId { get; set; }
        public string OldPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class FirstLoginRequest
    {
        public string Username { get; set; } = string.Empty;
        public string SetupPin { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
