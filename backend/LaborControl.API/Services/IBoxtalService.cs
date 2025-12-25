using LaborControl.API.Models;

namespace LaborControl.API.Services
{
    /// <summary>
    /// Interface pour le service d'expédition Boxtal
    /// Documentation API: https://api.boxtal.com/v1/documentation
    /// </summary>
    public interface IBoxtalService
    {
        /// <summary>
        /// Obtient un devis d'expédition pour une commande
        /// </summary>
        /// <param name="order">Commande à expédier</param>
        /// <param name="weightInGrams">Poids du colis en grammes (par défaut 500g pour 10 puces)</param>
        /// <returns>Liste des offres d'expédition disponibles</returns>
        Task<BoxtalQuoteResponse> GetShippingQuoteAsync(Order order, int weightInGrams = 500);

        /// <summary>
        /// Crée une expédition Boxtal pour une commande
        /// </summary>
        /// <param name="order">Commande à expédier</param>
        /// <param name="selectedOffer">Code de l'offre sélectionnée (ex: "colissimo-expert")</param>
        /// <param name="weightInGrams">Poids du colis en grammes</param>
        /// <returns>BoxtalShipment créé</returns>
        Task<BoxtalShipment> CreateShipmentAsync(Order order, string selectedOffer, int weightInGrams = 500);

        /// <summary>
        /// Récupère l'étiquette d'expédition
        /// </summary>
        /// <param name="shipmentId">ID de l'expédition</param>
        /// <returns>URL de téléchargement de l'étiquette</returns>
        Task<string> GetShippingLabelAsync(Guid shipmentId);

        /// <summary>
        /// Met à jour le statut de suivi d'une expédition
        /// </summary>
        /// <param name="shipmentId">ID de l'expédition</param>
        /// <returns>BoxtalShipment mis à jour</returns>
        Task<BoxtalShipment> UpdateTrackingStatusAsync(Guid shipmentId);

        /// <summary>
        /// Annule une expédition Boxtal
        /// </summary>
        /// <param name="shipmentId">ID de l'expédition</param>
        /// <returns>True si annulation réussie</returns>
        Task<bool> CancelShipmentAsync(Guid shipmentId);

        /// <summary>
        /// Trouve les points relais disponibles près d'une adresse
        /// </summary>
        /// <param name="postalCode">Code postal</param>
        /// <param name="city">Ville</param>
        /// <param name="country">Pays (par défaut FR)</param>
        /// <returns>Liste des points relais</returns>
        Task<List<BoxtalPickupPoint>> FindPickupPointsAsync(string postalCode, string city, string country = "FR");
    }

    /// <summary>
    /// Réponse de devis Boxtal
    /// </summary>
    public class BoxtalQuoteResponse
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<BoxtalOffer> Offers { get; set; } = new();
    }

    /// <summary>
    /// Offre d'expédition Boxtal
    /// </summary>
    public class BoxtalOffer
    {
        public string Code { get; set; } = string.Empty;
        public string CarrierName { get; set; } = string.Empty;
        public string ServiceName { get; set; } = string.Empty;
        public decimal PriceExclTax { get; set; }
        public decimal PriceInclTax { get; set; }
        public int DeliveryDelayInDays { get; set; }
        public DateTime? EstimatedDeliveryDate { get; set; }
        public string? Description { get; set; }
        public bool IsPickupPoint { get; set; }
    }

    /// <summary>
    /// Point relais Boxtal
    /// </summary>
    public class BoxtalPickupPoint
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public decimal DistanceInKm { get; set; }
        public List<string> OpeningHours { get; set; } = new();
    }
}
