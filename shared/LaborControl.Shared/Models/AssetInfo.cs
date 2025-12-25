namespace LaborControl.Shared.Models
{
    public class AssetInfo
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
        public string? Manufacturer { get; set; }
        public string? Model { get; set; }
        public string? SerialNumber { get; set; }
        public DateTime? InstallationDate { get; set; }
        public DateTime? LastMaintenanceDate { get; set; }
        public DateTime? NextMaintenanceDate { get; set; }
        public bool IsActive { get; set; }
        public int SubAssetsCount { get; set; }
        public int ControlPointsCount { get; set; }
    }
}
