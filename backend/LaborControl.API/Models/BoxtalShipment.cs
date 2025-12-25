using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    /// <summary>
    /// Représente une expédition Boxtal liée à une commande
    /// </summary>
    public class BoxtalShipment
    {
        public Guid Id { get; set; }

        /// <summary>
        /// Commande associée à cette expédition
        /// </summary>
        [Required]
        public Guid OrderId { get; set; }
        public Order? Order { get; set; }

        /// <summary>
        /// Référence Boxtal de l'expédition
        /// </summary>
        [MaxLength(100)]
        public string? BoxtalReference { get; set; }

        /// <summary>
        /// Offre Boxtal sélectionnée (ex: "colissimo", "chronopost", etc.)
        /// </summary>
        [MaxLength(50)]
        public string? SelectedOffer { get; set; }

        /// <summary>
        /// Code transporteur (ex: "COLISSIMO", "CHRONOPOST", "UPS")
        /// </summary>
        [MaxLength(50)]
        public string? CarrierCode { get; set; }

        /// <summary>
        /// Nom du service de livraison (ex: "Colissimo Expert", "Chronopost 13h")
        /// </summary>
        [MaxLength(200)]
        public string? ServiceName { get; set; }

        /// <summary>
        /// Numéro de suivi du colis
        /// </summary>
        [MaxLength(100)]
        public string? TrackingNumber { get; set; }

        /// <summary>
        /// URL de suivi du colis
        /// </summary>
        [MaxLength(500)]
        public string? TrackingUrl { get; set; }

        /// <summary>
        /// Prix HT de l'expédition en euros
        /// </summary>
        public decimal PriceExclTax { get; set; }

        /// <summary>
        /// Prix TTC de l'expédition en euros
        /// </summary>
        public decimal PriceInclTax { get; set; }

        /// <summary>
        /// Poids du colis en grammes
        /// </summary>
        public int WeightInGrams { get; set; }

        /// <summary>
        /// Statut de l'expédition Boxtal
        /// QUOTE_REQUESTED, QUOTE_RECEIVED, SHIPMENT_CREATED, LABEL_GENERATED, SHIPPED, DELIVERED, CANCELLED
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "QUOTE_REQUESTED";

        /// <summary>
        /// URL de téléchargement de l'étiquette d'expédition
        /// </summary>
        [MaxLength(500)]
        public string? LabelUrl { get; set; }

        /// <summary>
        /// Données JSON de la réponse Boxtal (pour debug/historique)
        /// </summary>
        public string? BoxtalResponse { get; set; }

        /// <summary>
        /// Message d'erreur si l'expédition a échoué
        /// </summary>
        [MaxLength(1000)]
        public string? ErrorMessage { get; set; }

        /// <summary>
        /// Date de création de l'expédition
        /// </summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// Date de dernière mise à jour
        /// </summary>
        public DateTime? UpdatedAt { get; set; }

        /// <summary>
        /// Date d'expédition effective
        /// </summary>
        public DateTime? ShippedAt { get; set; }

        /// <summary>
        /// Date de livraison estimée
        /// </summary>
        public DateTime? EstimatedDeliveryDate { get; set; }

        /// <summary>
        /// Date de livraison effective
        /// </summary>
        public DateTime? DeliveredAt { get; set; }
    }
}
