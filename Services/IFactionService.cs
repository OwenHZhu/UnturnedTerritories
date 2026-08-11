using System.Collections.Generic;
using TerritoryPlugin.Models;

namespace TerritoryPlugin.Services
{
    public interface IFactionService
    {
        string? GetFactionId(ulong steamId);
        void SetFactionId(ulong steamId, string factionId);
        bool RemoveFactionId(ulong steamId);
        IReadOnlyList<ulong> GetFactionMembers(string factionId);
        Faction? GetFactionByName(string factionName);
        Faction CreateFaction(string factionName);
    }
}