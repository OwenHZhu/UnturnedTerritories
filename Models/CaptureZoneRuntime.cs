namespace TerritoryPlugin.Models
{
    public enum CaptureState
    {
        Neutral,
        Capturing,
        Contested,
        Controlled
    }

    public class CaptureZoneRuntime
    {
        public CaptureZoneRuntime(CaptureZone definition)
        {
            Definition = definition;
        }

        public CaptureZone Definition { get; }

        public CaptureState State { get; set; } = CaptureState.Neutral;

        public string? OwnerFactionId { get; set; }

        public string? CapturingFactionId { get; set; }

        public float Progress { get; set; }
    }
}
