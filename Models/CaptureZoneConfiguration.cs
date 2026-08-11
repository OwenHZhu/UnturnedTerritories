namespace TerritoryPlugin.Models
{
    public class TerritoryConfiguration
    {
        public CaptureZoneConfiguration CaptureZones {get; set; } = new CaptureZoneConfiguration();
    }

    public class CaptureZoneConfiguration
    {
        public string TimeZoneId { get; set; } = "America/Santiago";
        public string ScoringStart { get; set; } = "00:00";
        public string ScoringEnd { get; set; } = "23:59";
    }
}