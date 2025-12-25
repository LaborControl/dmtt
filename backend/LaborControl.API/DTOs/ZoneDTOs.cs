namespace LaborControl.API.DTOs
{
    public class ZoneDto
    {
        public Guid Id { get; set; }
        public Guid SiteId { get; set; }
        public string SiteName { get; set; } = "";
        public string Name { get; set; } = "";
        public string FullPath { get; set; } = "";
        public string? Code { get; set; }
        public string? Type { get; set; }
        public string? Description { get; set; }
        public Guid? ParentZoneId { get; set; }
        public int Level { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public int AssetsCount { get; set; }
        public int ControlPointsCount { get; set; }
    }

    public class ZoneDetailDto
    {
        public Guid Id { get; set; }
        public Guid SiteId { get; set; }
        public string SiteName { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Code { get; set; }
        public string? Type { get; set; }
        public string? Description { get; set; }
        public Guid? ParentZoneId { get; set; }
        public string? ParentZoneName { get; set; }
        public int Level { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
        public List<ZoneDto> SubZones { get; set; } = new();
        public List<AssetDto> Assets { get; set; } = new();
        public List<ControlPointDto> ControlPoints { get; set; } = new();
    }

    public class CreateZoneRequest
    {
        public Guid SiteId { get; set; }
        public string Name { get; set; } = "";
        public string? Code { get; set; }
        public string? Type { get; set; }
        public string? Description { get; set; }
        public Guid? ParentZoneId { get; set; }
        public int DisplayOrder { get; set; } = 0;
    }

    public class UpdateZoneRequest
    {
        public string Name { get; set; } = "";
        public string? Code { get; set; }
        public string? Type { get; set; }
        public string? Description { get; set; }
        public Guid? ParentZoneId { get; set; }
        public int DisplayOrder { get; set; }
        public bool IsActive { get; set; }
    }

    public class ControlPointDto
    {
        public Guid Id { get; set; }
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public Guid? RfidChipId { get; set; }
        public bool HasNfcChip { get; set; }
    }
}
