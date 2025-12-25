namespace LaborControl.API.DTOs
{
    public class LoginResponse
    {
        public string Token { get; set; } = string.Empty;
        public bool RequiresPasswordChange { get; set; } = false;
        public UserDto User { get; set; } = new UserDto();
    }

    public class UserDto
    {
        public Guid Id { get; set; }
        public string? Email { get; set; }
        public string? Username { get; set; }
        public string Nom { get; set; } = string.Empty;
        public string Prenom { get; set; } = string.Empty;
        public string? Niveau { get; set; }
        public string Role { get; set; } = string.Empty;
        public Guid CustomerId { get; set; }
    }
}
