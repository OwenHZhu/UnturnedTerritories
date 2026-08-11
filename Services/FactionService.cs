using System;
using System.Collections.Generic;
using System.Linq;
using TerritoryPlugin.Models;

namespace TerritoryPlugin.Services
{
    public class FactionService : IFactionService
    {
        private readonly Dictionary<ulong, string> m_playerFactions =
            new Dictionary<ulong, string>();

        private readonly Dictionary<string, Faction> m_factions =
            new Dictionary<string, Faction>(StringComparer.OrdinalIgnoreCase);

        public string? GetFactionId(ulong steamId)
        {
            return m_playerFactions.TryGetValue(steamId, out var factionId)
                ? factionId
                : null;
        }

        public void SetFactionId(ulong steamId, string factionId)
        {
            if (string.IsNullOrWhiteSpace(factionId))
            {
                throw new ArgumentException(
                    "Faction ID must not be empty.",
                    nameof(factionId));
            }

            factionId = factionId.Trim();

            if (!m_factions.ContainsKey(factionId))
            {
                throw new InvalidOperationException(
                    $"Faction '{factionId}' does not exist.");
            }

            m_playerFactions[steamId] = factionId;
        }

        public bool RemoveFactionId(ulong steamId)
        {
            return m_playerFactions.Remove(steamId);
        }

        public IReadOnlyList<ulong> GetFactionMembers(string factionId)
        {
            if (string.IsNullOrWhiteSpace(factionId))
            {
                return Array.Empty<ulong>();
            }

            factionId = factionId.Trim();

            return m_playerFactions
                .Where(kvp => string.Equals(kvp.Value, factionId, StringComparison.OrdinalIgnoreCase))
                .Select(kvp => kvp.Key)
                .ToList();
        }

        public Faction? GetFactionByName(string factionName)
        {
            if (string.IsNullOrWhiteSpace(factionName))
            {
                return null;
            }

            factionName = factionName.Trim();

            return m_factions.TryGetValue(factionName, out var faction)
                ? faction
                : null;
        }

        public Faction CreateFaction(string factionName)
        {
            if (string.IsNullOrWhiteSpace(factionName))
            {
                throw new ArgumentException(
                    "Faction name must not be empty.",
                    nameof(factionName));
            }

            factionName = factionName.Trim();

            if (m_factions.ContainsKey(factionName))
            {
                throw new InvalidOperationException(
                    $"A faction with the name '{factionName}' already exists.");
            }

            var faction = new Faction
            {
                Id = factionName,
                Name = factionName
            };

            m_factions[faction.Id] = faction;

            return faction;
        }
    }
}
