namespace LaborControl.API.DTOs
{
    public class CreateSupplierRequest
    {
        public string Name { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = "FR";
        public string? Siret { get; set; }
        public string? VatNumber { get; set; }
        public string? TaxId { get; set; }
        public bool IsEuSupplier { get; set; }
        public string Website { get; set; } = string.Empty;
        public string PaymentTerms { get; set; } = "NET 30";
        public int LeadTimeDays { get; set; } = 7;
    }

    public class UpdateSupplierRequest
    {
        public string Name { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = "FR";
        public string? Siret { get; set; }
        public string? VatNumber { get; set; }
        public string? TaxId { get; set; }
        public bool IsEuSupplier { get; set; }
        public string Website { get; set; } = string.Empty;
        public string PaymentTerms { get; set; } = "NET 30";
        public int LeadTimeDays { get; set; } = 7;
        public bool IsActive { get; set; } = true;
    }

    public class SupplierResponse
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string ContactName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public string? Siret { get; set; }
        public string? VatNumber { get; set; }
        public string? TaxId { get; set; }
        public bool IsEuSupplier { get; set; }
        public string Website { get; set; } = string.Empty;
        public string PaymentTerms { get; set; } = string.Empty;
        public int LeadTimeDays { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CreateSupplierOrderRequest
    {
        public Guid SupplierId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime? ExpectedDeliveryDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string? Notes { get; set; }
        public List<CreateSupplierOrderLineRequest> Lines { get; set; } = new();
    }

    public class CreateSupplierOrderLineRequest
    {
        public string ProductType { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }

    public class UpdateSupplierOrderRequest
    {
        public DateTime? ExpectedDeliveryDate { get; set; }
        public string? Notes { get; set; }
    }

    public class SupplierOrderResponse
    {
        public Guid Id { get; set; }
        public Guid SupplierId { get; set; }
        public string OrderNumber { get; set; } = string.Empty;
        public DateTime OrderDate { get; set; }
        public DateTime? SentDate { get; set; }
        public DateTime? ExpectedDeliveryDate { get; set; }
        public DateTime? ReceivedDate { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? Notes { get; set; }
        public List<SupplierOrderLineResponse> Lines { get; set; } = new();
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class SupplierOrderLineResponse
    {
        public Guid Id { get; set; }
        public string ProductType { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public int ReceivedQuantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TotalPrice { get; set; }
    }

    public class SendSupplierOrderRequest
    {
        public DateTime? ExpectedDeliveryDate { get; set; }
    }

    public class ReceiveSupplierOrderRequest
    {
        public List<ReceiveSupplierOrderLineRequest> Lines { get; set; } = new();
    }

    public class ReceiveSupplierOrderLineRequest
    {
        public Guid LineId { get; set; }
        public int ReceivedQuantity { get; set; }
    }
}
