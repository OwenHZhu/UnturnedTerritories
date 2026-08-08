namespace TerritoryPlugin.Models
{

    public class Territory
    {
        public string Name { get; set; } = "";
    
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
    
        public float Radius { get; set; } = 100f;
    }
}