namespace LaborControl.API.DTOs
{
    public class CreateOrderRequest
    {
        public string DeliveryAddress { get; set; } = string.Empty;
        public string? DeliveryCity { get; set; }
        public string? DeliveryPostalCode { get; set; }
        public string? DeliveryCountry { get; set; } = "France";
        public string? Service { get; set; }
        public string? Notes { get; set; }
    }

    public class OrderResponse
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public Guid CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int ChipsQuantity { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? PackagingCode { get; set; }
        public string DeliveryAddress { get; set; } = string.Empty;
        public string? DeliveryCity { get; set; }
        public string? DeliveryPostalCode { get; set; }
        public string? DeliveryCountry { get; set; }
        public string? StripeCheckoutSessionId { get; set; }
        public string? CheckoutUrl { get; set; }
        public string? CarrierName { get; set; }
        public string? TrackingNumber { get; set; }
        public DateTime? ShippedAt { get; set; }
        public DateTime? DeliveredAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? Notes { get; set; }
    }

    public class ShipOrderRequest
    {
        public string CarrierName { get; set; } = string.Empty;
        public string TrackingNumber { get; set; } = string.Empty;
    }

    public class CreateCheckoutSessionRequest
    {
        public Guid OrderId { get; set; }
        public string SuccessUrl { get; set; } = string.Empty;
        public string CancelUrl { get; set; } = string.Empty;
    }

    public class CreateCheckoutSessionResponse
    {
        public bool Success { get; set; }
        public string? CheckoutUrl { get; set; }
        public string? SessionId { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class OrderDetailResponse
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? ProductType { get; set; }
        public int ChipsQuantity { get; set; }
        public decimal TotalAmount { get; set; }
        public string? DeliveryAddress { get; set; }
        public string? DeliveryCity { get; set; }
        public string? DeliveryPostalCode { get; set; }
        public string? Service { get; set; }
        public string? Notes { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? StripePaymentIntentId { get; set; }
        public string? PackagingCode { get; set; }

        // Statuts supplémentaires
        public string? DeliveryStatus { get; set; }
        public string? ChipsActivationStatus { get; set; }
        public int ChipsActivated { get; set; }
        public string? ChipsAssignmentStatus { get; set; }
        public int ChipsAssigned { get; set; }
    }

    public class ReceiveOrderRequest
    {
        public string PackagingCode { get; set; } = string.Empty;
    }

    public class ReceiveOrderResponse
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int ChipsReceived { get; set; }
    }

    public class ReceiveDeliveryRequest
    {
        public string Pkg { get; set; } = string.Empty;
    }

    public class RecentOrderDTO
    {
        public Guid Id { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public int ChipsQuantity { get; set; }
        public DateTime CreatedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
