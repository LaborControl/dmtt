using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.DTOs
{
    public class RegisterProfessionalRequest
    {
        [Required(ErrorMessage = "L'email est obligatoire")]
        [EmailAddress(ErrorMessage = "L'email n'est pas valide")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le mot de passe est obligatoire")]
        [MinLength(8, ErrorMessage = "Le mot de passe doit contenir au moins 8 caractères")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom de l'entreprise est obligatoire")]
        public string CompanyName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le SIRET est obligatoire")]
        public string Siret { get; set; } = string.Empty;

        [Required(ErrorMessage = "L'adresse est obligatoire")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le code postal est obligatoire")]
        public string PostalCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "La ville est obligatoire")]
        public string City { get; set; } = string.Empty;

        public string? Website { get; set; }

        public string? Phone { get; set; }

        [Required(ErrorMessage = "Le prénom est obligatoire")]
        public string FirstName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le nom est obligatoire")]
        public string LastName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Le titre du poste est obligatoire")]
        public string JobTitle { get; set; } = string.Empty;
    }

    public class RegisterProfessionalResponse
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public Guid? CustomerId { get; set; }
        public Guid? UserId { get; set; }
        public string? CompanyName { get; set; }
    }
}
