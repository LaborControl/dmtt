namespace LaborControl.Shared.Models
{
    public class ZoneInfo
    {
        public Guid Id { get; set; }
        public Guid SiteId { get; set; }
        public string Name { get; set; } = "";
        public string? Code { get; set; }
        public string? Type { get; set; }
        public string? Description { get; set; }
        public Guid? ParentZoneId { get; set; }
        public int Level { get; set; }
        public bool IsActive { get; set; }
        public int AssetsCount { get; set; }
        public int ControlPointsCount { get; set; }
    }
}
