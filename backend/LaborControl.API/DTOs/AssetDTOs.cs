namespace LaborControl.API.DTOs
{
    public class AssetDto
    {
        public Guid Id { get; set; }
        public Guid ZoneId { get; set; }
        public string ZoneName { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Code { get; set; }
        public string Type { get; set; } = "";
        public string? Category { get; set; }
        public string? Status { get; set; }
        public Guid? ParentAssetId { get; set; }
        public int Level { get; set; }
        public int DisplayOrder { get; set; }
        public string? Manufacturer { get; set; }
        public string? Model { get; set; }
        public string? SerialNumber { get; set; }
        public DateTime? InstallationDate { get; set; }
        public bool IsActive { get; set; }
        public int SubAssetsCount { get; set; }
        public int ControlPointsCount { get; set; }
    }

    public class AssetDetailDto
    {
        public Guid Id { get; set; }
        public Guid ZoneId { get; set; }
        public string ZoneName { get; set; } = "";
        public string Name { get; set; } = "";
        public string? Code { get; set; }
        public string Type { get; set; } = "";
        public string? Category { get; set; }
        public string? Status { get; set; }
        public Guid? ParentAssetId { get; set; }
        public string? ParentAssetName { get; set; }
        public int Level { get; set; }
        public int DisplayOrder { get; set; }
        public string? TechnicalData { get; set; }
        public string? Manufacturer { get; set; }
        public string? Model { get; set; }
        public string? SerialNumber { get; set; }
        public DateTime? InstallationDate { get; set; }
        public bool IsActive { get; set; }
        public List<AssetDto> SubAssets { get; set; } = new();
        public List<ControlPointDto> ControlPoints { get; set; } = new();
    }

    public class CreateAssetRequest
    {
        public Guid ZoneId { get; set; }
        public string Name { get; set; } = "";
        public string? Code { get; set; }
        public string Type { get; set; } = "";
        public string? Category { get; set; }
        public string? Status { get; set; }
        public Guid? ParentAssetId { get; set; }
        public int DisplayOrder { get; set; } = 0;
        public string? TechnicalData { get; set; }
        public string Manufacturer { get; set; } = "";
        public string Model { get; set; } = "";
        public string? SerialNumber { get; set; }
        public DateTime? InstallationDate { get; set; }
        public bool IsAICyrilleEnabled { get; set; } = true;
        public bool IsAIAimeeEnabled { get; set; } = true;
    }

    public class UpdateAssetRequest
    {
        public Guid ZoneId { get; set; }
        public string Name { get; set; } = "";
        public string? Code { get; set; }
        public string Type { get; set; } = "";
        public string? Category { get; set; }
        public string? Status { get; set; }
        public Guid? ParentAssetId { get; set; }
        public int DisplayOrder { get; set; }
        public string? TechnicalData { get; set; }
        public string Manufacturer { get; set; } = "";
        public string Model { get; set; } = "";
        public string? SerialNumber { get; set; }
        public DateTime? InstallationDate { get; set; }
        public bool IsActive { get; set; }
    }
}
