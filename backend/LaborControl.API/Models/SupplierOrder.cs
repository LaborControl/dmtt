namespace LaborControl.API.Models
{
    public class SupplierOrder
    {
        public Guid Id { get; set; }
        public Guid SupplierId { get; set; }
        public Supplier? Supplier { get; set; }

        // Numéro de commande unique
        public string OrderNumber { get; set; } = string.Empty;

        // Dates
        public DateTime OrderDate { get; set; } = DateTime.UtcNow;
        public DateTime? SentDate { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public DateTime? ReceivedDate { get; set; }

        // Montant
        public decimal TotalAmount { get; set; }

        // Statut : DRAFT, SENT, EN_TRANSIT, RECEIVED, CANCELLED
        public string Status { get; set; } = "DRAFT";

        // Notes
        public string? Notes { get; set; }

        // Relations
        public List<SupplierOrderLine> Lines { get; set; } = new();
        public List<RfidChip> RfidChips { get; set; } = new();

        // Audit
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
