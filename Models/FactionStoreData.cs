using System.Collections.Generic;

namespace TerritoryPlugin.Models
{
    public class FactionStoreData
    {
        public List<Faction> Factions { get; set; } = new List<Faction>();

        // SteamId -> FactionId
        public Dictionary<string, string> PlayerFactionMap { get; set; } =
            new Dictionary<string, string>();
    }
}
