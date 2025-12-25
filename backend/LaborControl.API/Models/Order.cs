using System.ComponentModel.DataAnnotations;

namespace LaborControl.API.Models
{
    public class Order
    {
        public Guid Id { get; set; }

        [Required]
        public Guid CustomerId { get; set; }
        public Customer? Customer { get; set; }

        [Required]
        [MaxLength(20)]
        public string OrderNumber { get; set; } = string.Empty;

        public int ChipsQuantity { get; set; } = 10; // 10 puces gratuites

        public decimal TotalAmount { get; set; } = 10.00m; // 10€ frais de port

        [MaxLength(50)]
        public string? ProductType { get; set; } = "pack_discovery"; // Type de produit commandé

        [Required]
        [MaxLength(50)]
        public string Status { get; set; } = "PENDING"; // PENDING, PAID, SHIPPED, DELIVERED, CANCELLED

        // Adresse de livraison
        [Required]
        [MaxLength(500)]
        public string DeliveryAddress { get; set; } = string.Empty;

        [MaxLength(100)]
        public string? DeliveryCity { get; set; }

        [MaxLength(10)]
        public string? DeliveryPostalCode { get; set; }

        [MaxLength(100)]
        public string? DeliveryCountry { get; set; } = "France";

        [MaxLength(200)]
        public string? Service { get; set; } // Service destinataire

        // Stripe
        public string? StripePaymentIntentId { get; set; }
        public string? StripeCheckoutSessionId { get; set; }

        // Tracking & Shipping
        public string? CarrierName { get; set; }        // "DPD", "Fedex", "GLS", "Colissimo", etc.
        public string? TrackingNumber { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }

        // Stock reservation (pour gérer le stock sans scan à la préparation)
        public bool IsStockReserved { get; set; } = false;  // true = puces physiquement sorties du stock
        public DateTime? PreparedAt { get; set; }            // Date de préparation de la commande

        // Code packaging unique pour cette commande (ex: PKG-2025-001)
        public string? PackagingCode { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public string? Notes { get; set; }

        // Facture PDF
        public string? InvoicePdfPath { get; set; }
        public DateTime? InvoiceGeneratedAt { get; set; }

        // Collection des puces RFID de cette commande
        public ICollection<RfidChip> RfidChips { get; set; } = new List<RfidChip>();
    }
}
