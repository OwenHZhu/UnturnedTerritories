using System.Collections.Generic;

namespace TerritoryPlugin.Models
{
    public class TerritoryConfiguration
    {
        public CaptureZoneConfiguration CaptureZones {get; set; } = new CaptureZoneConfiguration();
    }

    public class CaptureZoneConfiguration
    {
        public string TimeZoneId { get; set; } = "America/Santiago";
        public string ScoringStart { get; set; } = "18:30";
        public string ScoringEnd { get; set; } = "19:00";
        public List<CaptureZone> Zones { get; set; } = new List<CaptureZone>();
    }
}