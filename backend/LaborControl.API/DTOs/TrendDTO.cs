namespace LaborControl.API.DTOs
{
    public class TrendResponse
    {
        public int Total { get; set; }
        public int ThisMonth { get; set; }
        public int LastMonth { get; set; }
        public double PercentChange { get; set; }
    }
}
