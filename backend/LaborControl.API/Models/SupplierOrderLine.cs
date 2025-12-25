namespace LaborControl.API.Models
{
    public class SupplierOrderLine
    {
        public Guid Id { get; set; }
        public Guid SupplierOrderId { get; set; }
        public SupplierOrder? SupplierOrder { get; set; }

        // Type de puce
        public string ProductType { get; set; } = string.Empty; // NTAG213, NTAG215, etc.

        // Quantités
        public int Quantity { get; set; }
        public int ReceivedQuantity { get; set; } = 0;

        // Prix
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
