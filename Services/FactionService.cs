using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TerritoryPlugin.Models;
using Cysharp.Threading.Tasks;
using OpenMod.API.Plugins;

namespace TerritoryPlugin.Services
{
    public class FactionService : IFactionService
    {
        private const string FactionsDataKey = "Factions";

        private readonly Dictionary<ulong, string> m_playerFactions =
            new Dictionary<ulong, string>();

        private readonly Dictionary<string, Faction> m_factions =
            new Dictionary<string, Faction>(StringComparer.OrdinalIgnoreCase);

        private readonly Lazy<IPluginAccessor<TerritoryPlugin>> m_PluginAccessor;
        private readonly ILogger<FactionService> m_Logger;
        private readonly object m_lock = new object();

        public FactionService(Lazy<IPluginAccessor<TerritoryPlugin>> pluginAccessor, ILogger<FactionService> logger)
        {
            m_PluginAccessor = pluginAccessor;
            m_Logger = logger;
            LoadAsync().Forget();
        }

        private async UniTask LoadAsync()
        {
            try
            {
                var plugin = m_PluginAccessor.Value.Instance;
                if (plugin == null || !await plugin.DataStore.ExistsAsync(FactionsDataKey))
                {
                    return;
                }

                FactionStoreData? saved = await plugin.DataStore.LoadAsync<FactionStoreData>(FactionsDataKey);
                if (saved == null)
                {
                    return;
                }

                lock (m_lock)
                {
                    m_factions.Clear();
                    foreach (var faction in saved.Factions)
                    {
                        m_factions[faction.Id] = faction;
                    }

                    m_playerFactions.Clear();
                    foreach (var kvp in saved.PlayerFactionMap)
                    {
                        if (ulong.TryParse(kvp.Key, out var steamId))
                        {
                            m_playerFactions[steamId] = kvp.Value;
                        }
                    }
                }

                m_Logger.LogInformation("Loaded {FactionCount} factions and {MemberCount} mappings.", m_factions.Count, m_playerFactions.Count);
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to load faction data.");
            }
        }

        private async UniTask SaveAsync()
        {
            try
            {
                var plugin = m_PluginAccessor.Value.Instance;
                if (plugin == null)
                {
                    return;
                }

                FactionStoreData data;
                lock (m_lock)
                {
                    data = new FactionStoreData
                    {
                        Factions = m_factions.Values.ToList(),
                        PlayerFactionMap = m_playerFactions.ToDictionary(k => k.Key.ToString(), v => v.Value)
                    };
                }

                await plugin.DataStore.SaveAsync(FactionsDataKey, data);
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to save faction data.");
            }
        }

        public string? GetFactionId(ulong steamId)
        {
            lock (m_lock)
            {
                return m_playerFactions.TryGetValue(steamId, out var factionId)
                    ? factionId
                    : null;
            }
        }
        
        public string? GetFactionName(string factionId)
        {
            if (string.IsNullOrWhiteSpace(factionId))
            {
                return null;
            }

            factionId = factionId.Trim();

            lock (m_lock)
            {
                return m_factions.TryGetValue(factionId, out var faction)
                    ? faction.Name
                    : null;
            }
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

            lock (m_lock)
            {
                if (!m_factions.ContainsKey(factionId))
                {
                    throw new InvalidOperationException(
                        $"Faction '{factionId}' does not exist.");
                }

                m_playerFactions[steamId] = factionId;
            }

            SaveAsync().Forget();
        }

        public bool RemoveFactionId(ulong steamId)
        {
            bool removed;
            lock (m_lock)
            {
                removed = m_playerFactions.Remove(steamId);
            }

            if (removed)
            {
                SaveAsync().Forget();
            }

            return removed;
        }

        public IReadOnlyList<ulong> GetFactionMembers(string factionId)
        {
            if (string.IsNullOrWhiteSpace(factionId))
            {
                return Array.Empty<ulong>();
            }

            factionId = factionId.Trim();

            lock (m_lock)
            {
                return m_playerFactions
                    .Where(kvp => string.Equals(kvp.Value, factionId, StringComparison.OrdinalIgnoreCase))
                    .Select(kvp => kvp.Key)
                    .ToList();
            }
        }

        public Faction? GetFactionByName(string factionName)
        {
            if (string.IsNullOrWhiteSpace(factionName))
            {
                return null;
            }

            factionName = factionName.Trim();

            lock (m_lock)
            {
                return m_factions.TryGetValue(factionName, out var faction)
                    ? faction
                    : null;
            }
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

            lock (m_lock)
            {
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

                SaveAsync().Forget();

                return faction;
            }
        }

        public bool DeleteFaction(string factionName)
        {
            if (string.IsNullOrWhiteSpace(factionName))
            {
                return false;
            }

            factionName = factionName.Trim();

            lock (m_lock)
            {
                if(!m_factions.TryGetValue(factionName, out var faction))
                {
                    return false;
                }

                foreach (var memberSteamId in GetFactionMembers(faction.Id).ToList())
                {
                    m_playerFactions.Remove(memberSteamId);
                }
                m_factions.Remove(factionName);
            }

            SaveAsync().Forget();
            return true;
        }
    }
}
