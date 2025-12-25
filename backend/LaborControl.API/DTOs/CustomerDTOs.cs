namespace LaborControl.API.DTOs
{
    public class CreateCustomerRequest
    {
        public string Name { get; set; } = string.Empty;

        // TODO: Sector supprimé du modèle Customer - maintenant au niveau des Teams
        // Ce champ est conservé temporairement pour compatibilité API mais n'est plus persisté
        public string Sector { get; set; } = "EHPAD";

        public string SubscriptionPlan { get; set; } = "free";
        public string? ContactName { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? Address { get; set; }
        public string? Siret { get; set; }
        public string? Description { get; set; }
    }

    public class CustomerResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;

        // TODO: Sector supprimé du modèle Customer - maintenant au niveau des Teams
        // Ce champ retourne une valeur par défaut pour compatibilité API
        public string Sector { get; set; } = string.Empty;

        public string SubscriptionPlan { get; set; } = string.Empty;
        public string? ContactName { get; set; }
        public string? ContactEmail { get; set; }
        public string? ContactPhone { get; set; }
        public string? Address { get; set; }
        public string? Siret { get; set; }
        public string? Description { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SubscriptionDTO
    {
        public Guid CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string SubscriptionPlan { get; set; } = string.Empty;
        public DateTime? ActivatedAt { get; set; }
        public string? StripeSubscriptionId { get; set; }
        public string Status { get; set; } = "active";
    }
}
