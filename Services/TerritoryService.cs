using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using OpenMod.API.Plugins;
using SDG.Unturned;
using TerritoryPlugin.Models;
using Cysharp.Threading.Tasks;

namespace TerritoryPlugin.Services
{
    public class TerritoryService
    {
        private const string TerritoriesDataKey = "Territories";

        private readonly List<Territory> m_Territories =
            new List<Territory>();
        private readonly IFactionService m_FactionService;
        private readonly Lazy<IPluginAccessor<TerritoryPlugin>> m_PluginAccessor;
        private readonly ILogger<TerritoryService> m_Logger;
        private readonly object m_lock = new object();

        public TerritoryService(IFactionService factionService, Lazy<IPluginAccessor<TerritoryPlugin>> pluginAccessor, ILogger<TerritoryService> logger)
        {
            m_FactionService = factionService ?? throw new ArgumentNullException(nameof(factionService));
            m_PluginAccessor = pluginAccessor;
            m_Logger = logger;
            LoadAsync().Forget();
        }

        public IReadOnlyList<Territory> Territories
        {
            get
            {
                lock (m_lock)
                {
                    return m_Territories.ToList();
                }
            }
        }

        public bool HasFactionTerritory(string factionId)
        {
            if (string.IsNullOrWhiteSpace(factionId))
            {
                return false;
            }

            factionId = factionId.Trim();

            lock (m_lock)
            {
                foreach (var territory in m_Territories)
                {
                    if (string.Equals(territory.FactionId, factionId, StringComparison.OrdinalIgnoreCase))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public void AddTerritory(Territory territory)
        {
            if (territory == null)
            {
                throw new ArgumentNullException(nameof(territory));
            }

            if (!string.IsNullOrWhiteSpace(territory.FactionId)
                && HasFactionTerritory(territory.FactionId))
            {
                throw new InvalidOperationException(
                    $"Faction '{territory.FactionId}' already owns a territory.");
            }

            lock (m_lock)
            {
                m_Territories.Add(territory);
            }

            SaveAsync().Forget();
        }

        public Territory? GetTerritoryAt(float x, float z)
        {
            lock (m_lock)
            {
                foreach (var territory in m_Territories)
                {
                    float dx = x - territory.X;
                    float dz = z - territory.Z;

                    float distanceSquared =
                        (dx * dx) + (dz * dz);

                    float radiusSquared =
                        territory.Radius * territory.Radius;

                    if (distanceSquared <= radiusSquared)
                    {
                        return territory;
                    }
                }
            }

            return null;
        }

        public bool CanPlayerBuild(ulong steamId, float x, float z)
        {
            string? playerFaction = m_FactionService.GetFactionId(steamId);

            Territory? territory = GetTerritoryAt(x, z); //position.X/Z might be wrong check against testing
            if (territory != null && !string.IsNullOrEmpty(territory.FactionId))
            {
                return string.Equals(territory.FactionId, playerFaction, StringComparison.OrdinalIgnoreCase);
            }

            return false;
        }

        private async UniTask LoadAsync()
        {
            try
            {
                var plugin = m_PluginAccessor.Value.Instance;
                if (plugin == null || !await plugin.DataStore.ExistsAsync(TerritoriesDataKey))
                {
                    return;
                }

                TerritoriesStoreData? saved = await plugin.DataStore.LoadAsync<TerritoriesStoreData>(TerritoriesDataKey);
                if (saved == null)
                {
                    return;
                }

                lock (m_lock)
                {
                    m_Territories.Clear();
                    m_Territories.AddRange(saved.Territories);
                }

                m_Logger.LogInformation("Loaded {Count} territories.", saved.Territories.Count);
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to load territories.");
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

                TerritoriesStoreData data;
                lock (m_lock)
                {
                    data = new TerritoriesStoreData
                    {
                        Territories = m_Territories.ToList()
                    };
                }

                await plugin.DataStore.SaveAsync(TerritoriesDataKey, data);
            }
            catch (Exception ex)
            {
                m_Logger.LogError(ex, "Failed to save territories.");
            }
        }
    }
}