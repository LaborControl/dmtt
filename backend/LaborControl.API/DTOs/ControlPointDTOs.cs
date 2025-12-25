namespace LaborControl.API.DTOs
{
    public class CreateControlPointRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? LocationDescription { get; set; }
        public string? MeasurementType { get; set; } // Deprecated - no longer used

        // Rattachement à une Zone OU un Asset (au moins un des deux)
        public Guid? ZoneId { get; set; }
        public Guid? AssetId { get; set; }
    }

    public class UpdateControlPointRequest
    {
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? LocationDescription { get; set; }
        public string? MeasurementType { get; set; } // Deprecated - no longer used

        public Guid? ZoneId { get; set; }
        public Guid? AssetId { get; set; }
    }

    public class ControlPointResponse
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string? LocationDescription { get; set; }
        public string? MeasurementType { get; set; } // Deprecated - no longer used

        public Guid? RfidChipId { get; set; }
        public string? RfidChipCode { get; set; } // Code de la puce affectée

        public Guid? ZoneId { get; set; }
        public string? ZoneName { get; set; }
        public Guid? SiteId { get; set; } // Site de la zone

        public Guid? AssetId { get; set; }
        public string? AssetName { get; set; }

        public Guid CustomerId { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsActive { get; set; }
    }

    public class AssignRfidChipRequest
    {
        public Guid RfidChipId { get; set; }
    }
}
