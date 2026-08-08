namespace TerritoryPlugin.Models
{
    public class CaptureZone
    {
        public string Name { get; set; } = "";
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Radius { get; set; } = 100f;
        public int Weight { get; set; } = 1;

    }
}