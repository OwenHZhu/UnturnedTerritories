using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using OpenMod.API.Plugins;
using SDG.Unturned;
using TerritoryPlugin.Models;
using UnityEngine;

namespace TerritoryPlugin.Services
{
    public class CaptureZoneService
    {
        private const string FactionScoresDataKey = "faction_scores";

        private readonly TerritoryService m_TerritoryService;
        private readonly ILogger<CaptureZoneService> m_Logger;
        private readonly float m_ScoringDurationSeconds;
        private readonly Lazy<IPluginAccessor<TerritoryPlugin>> m_PluginAccessor;

        private readonly Dictionary<ulong, Territory?> m_PlayerTerritories =
            new Dictionary<ulong, Territory?>();

        private readonly List<CaptureZoneRuntime> m_CaptureZones =
            new List<CaptureZoneRuntime>();

        private readonly Dictionary<string, int> m_FactionRewards =
            new Dictionary<string, int>();

        public IReadOnlyList<CaptureZoneRuntime> CaptureZones =>
            m_CaptureZones;

        public IReadOnlyDictionary<string, int> FactionRewards =>
            m_FactionRewards;

        public float ScoringDurationSeconds =>
            m_ScoringDurationSeconds;

        public CaptureZoneService(
            TerritoryService territoryService,
            IConfiguration configuration,
            Lazy<IPluginAccessor<TerritoryPlugin>> pluginAccessor,
            ILogger<CaptureZoneService> logger)
        {
            m_TerritoryService = territoryService;
            m_Logger = logger;
            m_PluginAccessor = pluginAccessor;
            m_ScoringDurationSeconds = configuration.GetValue(
                "capture_zones:scoring_duration_seconds",
                60f);

            if (m_ScoringDurationSeconds <= 0f)
            {
                m_ScoringDurationSeconds = 60f;
            }
        }

        public async UniTask StartAsync(
            CancellationToken cancellationToken)
        {
            m_Logger.LogInformation(
                "CaptureZoneService started.");

            await LoadFactionRewardsAsync();

            while (!cancellationToken.IsCancellationRequested)
            {
                CheckPlayers();

                await UniTask.Delay(
                    TimeSpan.FromMilliseconds(500),
                    cancellationToken: cancellationToken);
            }
        }

        public CaptureZoneRuntime AddCaptureZone(CaptureZone captureZone)
        {
            var runtime = new CaptureZoneRuntime(captureZone);
            m_CaptureZones.Add(runtime);

            m_Logger.LogInformation(
                "Capture zone {Zone} created at {X:F1}, {Z:F1} with a {Radius:F0}m radius. " +
                "Scoring ends in {Duration:F0} seconds.",
                captureZone.Name,
                captureZone.X,
                captureZone.Z,
                captureZone.Radius,
                m_ScoringDurationSeconds);

            return runtime;
        }

        private static string? GetFactionId(SteamPlayer player)
        {
            ulong groupId = player.playerID.group.m_SteamID;

            return groupId == 0
                ? null
                : groupId.ToString();
        }

        private void CheckPlayers()
        {
            foreach (SteamPlayer steamPlayer in Provider.clients)
            {
                Vector3 position =
                    steamPlayer.player.transform.position;

                Territory? currentTerritory =
                    m_TerritoryService.GetTerritoryAt(
                        position.x,
                        position.z);

                ulong steamId =
                    steamPlayer.playerID.steamID.m_SteamID;

                m_PlayerTerritories.TryGetValue(
                    steamId,
                    out Territory? previousTerritory);

                if (currentTerritory != previousTerritory)
                {
                    if (previousTerritory != null)
                    {
                        m_Logger.LogInformation(
                            "{Player} left {Territory}",
                            steamPlayer.playerID.characterName,
                            previousTerritory.Name);
                    }

                    if (currentTerritory != null)
                    {
                        m_Logger.LogInformation(
                            "{Player} entered {Territory}",
                            steamPlayer.playerID.characterName,
                            currentTerritory.Name);
                    }

                    m_PlayerTerritories[steamId] =
                        currentTerritory;
                }
            }

            UpdateCaptureZones();
        }

        private void UpdateCaptureZones()
        {
            foreach (CaptureZoneRuntime zone in m_CaptureZones)
            {
                var playersPerFaction = new Dictionary<string, int>();

                foreach (SteamPlayer steamPlayer in Provider.clients)
                {
                    Vector3 position =
                        steamPlayer.player.transform.position;

                    float dx = position.x - zone.Definition.X;
                    float dz = position.z - zone.Definition.Z;

                    if ((dx * dx) + (dz * dz) <=
                        zone.Definition.Radius * zone.Definition.Radius)
                    {
                        string? factionId = GetFactionId(steamPlayer);

                        if (factionId != null)
                        {
                            playersPerFaction.TryGetValue(
                                factionId,
                                out int playerCount);

                            playersPerFaction[factionId] = playerCount + 1;
                        }
                    }
                }

                UpdateZone(zone, playersPerFaction, 0.5f);
            }
        }

        public CaptureZoneRuntime? GetCaptureZoneAt(float x, float z)
        {
            foreach (CaptureZoneRuntime zone in m_CaptureZones)
            {
                float dx = x - zone.Definition.X;
                float dz = z - zone.Definition.Z;

                if ((dx * dx) + (dz * dz) <=
                    zone.Definition.Radius * zone.Definition.Radius)
                {
                    return zone;
                }
            }

            return null;
        }

        public IReadOnlyList<KeyValuePair<string, int>> GetFactionLeaderboard(
            int maximumEntries = 5)
        {
            return m_FactionRewards
                .OrderByDescending(faction => faction.Value)
                .ThenBy(faction => faction.Key)
                .Take(maximumEntries)
                .ToArray();
        }

        public IReadOnlyList<KeyValuePair<string, int>> GetZoneLeaderboard(
            CaptureZoneRuntime zone,
            int maximumEntries = 5)
        {
            return zone.FactionScores
                .OrderByDescending(faction => faction.Value)
                .ThenBy(faction => faction.Key)
                .Take(maximumEntries)
                .ToArray();
        }

        public float GetRemainingSeconds(CaptureZoneRuntime zone)
        {
            return Math.Max(
                0f,
                m_ScoringDurationSeconds - zone.ElapsedSeconds);
        }

        private void UpdateZone(
            CaptureZoneRuntime zone,
            IReadOnlyDictionary<string, int> playersPerFaction,
            float elapsedSeconds)
        {
            if (zone.State == CaptureState.Finished)
            {
                return;
            }

            float remainingSeconds = Math.Max(
                0f,
                m_ScoringDurationSeconds - zone.ElapsedSeconds);

            float scoringSeconds = Math.Min(
                elapsedSeconds,
                remainingSeconds);

            zone.ElapsedSeconds += scoringSeconds;
            zone.ScoreTickAccumulator += scoringSeconds;

            int wholeSeconds = (int)zone.ScoreTickAccumulator;

            if (wholeSeconds > 0)
            {
                zone.ScoreTickAccumulator -= wholeSeconds;

                foreach (KeyValuePair<string, int> faction in playersPerFaction)
                {
                    zone.FactionScores.TryGetValue(
                        faction.Key,
                        out int currentScore);

                    zone.FactionScores[faction.Key] = currentScore +
                        (faction.Value * wholeSeconds);
                }
            }

            if (zone.ElapsedSeconds >= m_ScoringDurationSeconds)
            {
                FinishZone(zone);
            }
        }

        private void FinishZone(CaptureZoneRuntime zone)
        {
            string? leadingFaction = null;
            int leadingScore = 0;
            bool isTie = false;

            foreach (KeyValuePair<string, int> faction in zone.FactionScores)
            {
                if (leadingFaction == null || faction.Value > leadingScore)
                {
                    leadingFaction = faction.Key;
                    leadingScore = faction.Value;
                    isTie = false;
                }
                else if (faction.Value == leadingScore)
                {
                    isTie = true;
                }
            }

            zone.State = CaptureState.Finished;

            if (leadingFaction == null || isTie)
            {
                m_Logger.LogInformation(
                    "Capture zone {Zone} ended with no winner.",
                    zone.Definition.Name);

                return;
            }

            zone.WinningFactionId = leadingFaction;

            m_FactionRewards.TryGetValue(
                leadingFaction,
                out int currentReward);

            m_FactionRewards[leadingFaction] = currentReward +
                zone.Definition.Weight;

            SaveFactionRewardsAsync().Forget();

            m_Logger.LogInformation(
                "{Faction} won {Zone} with {Score} player-seconds and earned {Reward} faction points.",
                leadingFaction,
                zone.Definition.Name,
                leadingScore,
                zone.Definition.Weight);
        }

        private async UniTask LoadFactionRewardsAsync()
        {
            TerritoryPlugin? plugin = m_PluginAccessor.Value.Instance;

            if (plugin == null ||
                !await plugin.DataStore.ExistsAsync(FactionScoresDataKey))
            {
                return;
            }

            FactionScoreData? savedScores = await plugin.DataStore
                .LoadAsync<FactionScoreData>(FactionScoresDataKey);

            if (savedScores == null)
            {
                return;
            }

            foreach (KeyValuePair<string, int> score in savedScores.Scores)
            {
                m_FactionRewards[score.Key] = score.Value;
            }

            m_Logger.LogInformation(
                "Loaded scores for {FactionCount} factions.",
                m_FactionRewards.Count);
        }

        private async UniTask SaveFactionRewardsAsync()
        {
            TerritoryPlugin? plugin = m_PluginAccessor.Value.Instance;

            if (plugin == null)
            {
                return;
            }

            await plugin.DataStore.SaveAsync(
                FactionScoresDataKey,
                new FactionScoreData
                {
                    Scores = new Dictionary<string, int>(m_FactionRewards)
                });
        }
    }
}
