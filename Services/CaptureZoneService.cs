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
        private readonly Lazy<IPluginAccessor<TerritoryPlugin>> m_PluginAccessor;
        private readonly TimeZoneInfo m_ScheduleTimeZone;
        private readonly TimeSpan m_ScoringStart;
        private readonly TimeSpan m_ScoringEnd;

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

        public CaptureState CurrentScheduleState =>
            GetCurrentScheduleState();

        public CaptureZoneService(
            TerritoryService territoryService,
            IConfiguration configuration,
            Lazy<IPluginAccessor<TerritoryPlugin>> pluginAccessor,
            ILogger<CaptureZoneService> logger)
        {
            m_TerritoryService = territoryService;
            m_Logger = logger;
            m_PluginAccessor = pluginAccessor;
            m_ScheduleTimeZone = GetScheduleTimeZone(configuration);
            m_ScoringStart = GetScheduledTime(configuration, "capture_zones:scoring_start", "19:30");
            m_ScoringEnd = GetScheduledTime(configuration, "capture_zones:scoring_end", "20:00");
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
                "Daily scoring runs from {Start} to {End}.",
                captureZone.Name,
                captureZone.X,
                captureZone.Z,
                captureZone.Radius,
                m_ScoringStart,
                m_ScoringEnd);

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
            CaptureState scheduleState = GetCurrentScheduleState();

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

                UpdateZone(
                    zone,
                    playersPerFaction,
                    scheduleState,
                    0.5f);
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
            if (GetCurrentScheduleState() != CaptureState.Scoring)
            {
                return 0f;
            }

            return (float)GetSecondsUntil(
                GetScheduleLocalTime(),
                m_ScoringEnd);
        }

        private void UpdateZone(
            CaptureZoneRuntime zone,
            IReadOnlyDictionary<string, int> playersPerFaction,
            CaptureState scheduleState,
            float elapsedSeconds)
        {
            if (scheduleState != CaptureState.Scoring)
            {
                if (zone.State == CaptureState.Scoring)
                {
                    FinishZone(zone);
                }

                if (zone.State != CaptureState.Finished)
                {
                    zone.State = scheduleState;
                }

                return;
            }

            if (zone.State != CaptureState.Scoring)
            {
                StartScoringRound(zone);
            }

            zone.ScoreTickAccumulator += elapsedSeconds;

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

        }

        private void StartScoringRound(CaptureZoneRuntime zone)
        {
            zone.FactionScores.Clear();
            zone.WinningFactionId = null;
            zone.ScoreTickAccumulator = 0f;
            zone.State = CaptureState.Scoring;

            m_Logger.LogInformation(
                "Scoring started for capture zone {Zone}.",
                zone.Definition.Name);
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

        private TimeZoneInfo GetScheduleTimeZone(
            IConfiguration configuration)
        {
            string? timeZoneId = configuration[
                "capture_zones:time_zone_id"];

            if (string.IsNullOrWhiteSpace(timeZoneId))
            {
                return TimeZoneInfo.Local;
            }

            try
            {
                return TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            }
            catch (TimeZoneNotFoundException)
            {
                m_Logger.LogWarning(
                    "Timezone {Timezone} was not found. Using server local time.",
                    timeZoneId);
                return TimeZoneInfo.Local;
            }
            catch (InvalidTimeZoneException)
            {
                m_Logger.LogWarning(
                    "Timezone {Timezone} is invalid. Using server local time.",
                    timeZoneId);
                return TimeZoneInfo.Local;
            }
        }

        private TimeSpan GetScheduledTime(
            IConfiguration configuration,
            string configurationKey,
            string fallback)
        {
            string value = configuration[configurationKey] ?? fallback;

            if (TimeSpan.TryParse(value, out TimeSpan scheduledTime))
            {
                return scheduledTime;
            }

            m_Logger.LogWarning(
                "Invalid scheduled time {Value} for {Key}. Using {Fallback}.",
                value,
                configurationKey,
                fallback);

            return TimeSpan.Parse(fallback);
        }

        private CaptureState GetCurrentScheduleState()
        {
            TimeSpan currentTime = GetScheduleLocalTime();

            if (IsWithinWindow(
                currentTime,
                m_ScoringStart,
                m_ScoringEnd))
            {
                return CaptureState.Scoring;
            }

            return CaptureState.Inactive;
        }

        private TimeSpan GetScheduleLocalTime()
        {
            return TimeZoneInfo.ConvertTime(
                DateTimeOffset.UtcNow,
                m_ScheduleTimeZone).TimeOfDay;
        }

        private static bool IsWithinWindow(
            TimeSpan currentTime,
            TimeSpan startTime,
            TimeSpan endTime)
        {
            if (startTime < endTime)
            {
                return currentTime >= startTime && currentTime < endTime;
            }

            return currentTime >= startTime || currentTime < endTime;
        }

        private static double GetSecondsUntil(
            TimeSpan currentTime,
            TimeSpan endTime)
        {
            TimeSpan remaining = endTime - currentTime;

            if (remaining <= TimeSpan.Zero)
            {
                remaining += TimeSpan.FromDays(1);
            }

            return remaining.TotalSeconds;
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
