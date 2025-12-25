namespace LaborControl.API.Models
{
    public class Supplier
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;

        // Adresse
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = "FR"; // ISO 3166-1 alpha-2

        // Identifiants fiscaux (un seul requis selon pays)
        public string? Siret { get; set; }              // France (14 chiffres) - nullable
        public string? VatNumber { get; set; }          // UE (FR12345678901, DE123456789, etc.)
        public string? TaxId { get; set; }              // Hors UE (générique)
        public bool IsEuSupplier { get; set; }          // Flag UE pour TVA intracommunautaire

        // Informations légales
        public string Website { get; set; } = string.Empty;

        // Conditions commerciales
        public string PaymentTerms { get; set; } = "NET 30"; // NET 30, NET 60, etc.
        public int LeadTimeDays { get; set; } = 7; // Délai de livraison en jours

        // Statut
        public bool IsActive { get; set; } = true;

        // Relations
        public List<SupplierOrder> Orders { get; set; } = new();

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
