using System.Collections.Generic;

namespace TerritoryPlugin.Models
{
    public class FactionScoreData
    {
        public Dictionary<string, int> Scores { get; set; } =
            new Dictionary<string, int>();
    }
}
