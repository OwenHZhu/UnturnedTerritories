using System;
using System.Linq;
using System.Threading;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using SDG.Unturned;
using TerritoryPlugin.Models;
using UnityEngine;
using Cysharp.Threading.Tasks;
using OpenMod.API.Plugins;

namespace TerritoryPlugin.Services
{
    public class CaptureZoneService
    {
        private CaptureZoneConfiguration m_Configuration;
        private readonly IFactionService m_FactionService;
        private readonly List<CaptureZoneRuntime> m_CaptureZones = new List<CaptureZoneRuntime>();
        private readonly object m_stateLock = new object();
        private readonly ILogger<CaptureZoneService> m_Logger;
        private readonly Dictionary<ulong, CaptureZoneRuntime?> m_PlayerZones = new Dictionary<ulong, CaptureZoneRuntime?>();
        public IReadOnlyList<CaptureZoneRuntime> CaptureZonesList
        {
            get
            {
                lock (m_stateLock)
                {
                    return m_CaptureZones.ToArray();
                }
            }
        }
        private readonly Lazy<IPluginAccessor<TerritoryPlugin>> m_PluginAccessor;

        private const string FactionScoresDataKey = "FactionScores";
        private readonly Dictionary<string, int> m_FactionRewards =
            new Dictionary<string, int>();        public IReadOnlyDictionary<string, int> FactionRewards
        {
            get
            {
                lock (m_stateLock)
                {
                    return new Dictionary<string, int>(m_FactionRewards);
                }
            }
        }


        public CaptureZoneService(
        IFactionService factionService, 
        ILogger<CaptureZoneService> logger,
        Lazy<IPluginAccessor<TerritoryPlugin>> pluginAccessor)
        {
            m_Configuration = new CaptureZoneConfiguration();
            m_FactionService = factionService;
            m_Logger = logger;
            m_PluginAccessor = pluginAccessor;
        }

        public void SetConfiguration(CaptureZoneConfiguration configuration)
        {
            m_Configuration = configuration;
        }

        public async UniTask StartAsync(CancellationToken cancellationToken)
        {
            m_Logger.LogInformation("Starting capture zone service.");
            
            // Load preset zones from configuration
            if (m_Configuration?.Zones != null)
            {
                foreach (var zone in m_Configuration.Zones)
                {
                    AddCaptureZone(zone);
                    m_Logger.LogInformation("Loaded preset zone: {Zone}", zone.Name);
                }
            }
            
            await LoadFactionRewardsAsync();
            while (!cancellationToken.IsCancellationRequested)
            {
                UpdateCaptureZones();
                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);
            }
        }

        private async UniTask LoadFactionRewardsAsync()
        {
            TerritoryPlugin? plugin = m_PluginAccessor.Value.Instance;
            if (plugin == null || !await plugin.DataStore.ExistsAsync(FactionScoresDataKey))
            {
                return;
            }

            FactionScoreData? savedScores = await plugin.DataStore.LoadAsync<FactionScoreData>(FactionScoresDataKey);
            if (savedScores == null)
            {
                return;
            }

            foreach (KeyValuePair<string, int> score in savedScores.Scores)
            {
                m_FactionRewards[score.Key] = score.Value;
            }

            m_Logger.LogInformation(
                "Loaded scores for {FactionCount} factions.", m_FactionRewards.Count);
            

        }

        private async UniTask SaveFactionScoresAsync()
        {
            TerritoryPlugin? plugin = m_PluginAccessor.Value.Instance;
            if (plugin == null)
            {
                return;
            }

            Dictionary<string, int> scores;
            lock (m_stateLock)
            {
                scores = new Dictionary<string, int>(m_FactionRewards);
            }

            await plugin.DataStore.SaveAsync(
                FactionScoresDataKey, new FactionScoreData
                {
                    Scores = scores
                }
            );
        }

        public void AddCaptureZone(CaptureZone zone)
        {
            var runtime = new CaptureZoneRuntime(zone);
            lock (m_stateLock)
            {
                m_CaptureZones.Add(runtime);
            }
        }

        public bool RemoveCaptureZone(string zoneName)
        {
            lock (m_stateLock)
            {
                var zone = m_CaptureZones.FirstOrDefault(z =>
                    string.Equals(z.Definition.Name, zoneName, StringComparison.OrdinalIgnoreCase));

                if (zone == null)
                    return false;

                return m_CaptureZones.Remove(zone);
            }
        }

        public CaptureZoneRuntime? GetCaptureZoneAt(float x, float z)
        {
            foreach (var runtime in m_CaptureZones)
            {
                var zone = runtime.Definition;
                float dx = x - zone.X;
                float dz = z - zone.Z;

                float distanceSquared = (dx * dx) + (dz * dz);
                float radiusSquared = zone.Radius * zone.Radius;

                if (distanceSquared <= radiusSquared)
                {
                    return runtime;
                }
            }

            return null;
        }

        public string GetCaptureWindow()
        {
            var startTime = TimeSpan.Parse(m_Configuration.ScoringStart);
            var endTime = TimeSpan.Parse(m_Configuration.ScoringEnd);
            return $"Starttime: {startTime} EndTime: {endTime}";
        }

        public string GetCurrentZoneScores(CaptureZoneRuntime zone)
        {
            if (zone == null) throw new ArgumentNullException(nameof(zone));

            var header = $"Zone: {zone.Definition.Name}";

            if (zone.FactionScores.Count == 0)
            {
                return $"{header}{Environment.NewLine}No faction scores have been recorded for this zone yet.";
            }

            return $"{header}{Environment.NewLine}" + string.Join(Environment.NewLine,
                zone.FactionScores
                    .OrderByDescending(kvp => kvp.Value)
                    .Select(kvp => $"{kvp.Key}: {kvp.Value}"));
        }

        private bool IsCaptureWindowOpen()
        {
            var currentTime = DateTime.Now.TimeOfDay;
            var startTime = TimeSpan.Parse(m_Configuration.ScoringStart);
            var endTime = TimeSpan.Parse(m_Configuration.ScoringEnd);

            return currentTime >= startTime && currentTime <= endTime;
        }

        public void SetCaptureZoneState(CaptureState state)
        {
            foreach (var runtime in m_CaptureZones)
            {
                runtime.State = state;
            }
        }

        private CaptureState GetCurrentZoneState()
        {
            if (IsCaptureWindowOpen())
            {
                return CaptureState.Scoring;
            }
            return CaptureState.Inactive;
        }

        private void UpdateCaptureZones()
        {
            CaptureState scheduleState = GetCurrentZoneState();
            CaptureZoneRuntime[] zones;

            lock (m_stateLock)
            {
                zones = m_CaptureZones.ToArray();
            }

            foreach (CaptureZoneRuntime zone in zones)
            {
                var playersPerFaction = new Dictionary<string, int>();

                foreach (SteamPlayer steamPlayer in Provider.clients)
                {
                    Vector3 position = steamPlayer.player.transform.position;
                    float dx = position.x - zone.Definition.X;
                    float dz = position.z - zone.Definition.Z;
                    if ((dx * dx) + (dz * dz) <= (zone.Definition.Radius * zone.Definition.Radius))
                    {
                        string? factionId = getFactionId(steamPlayer);
                        if (factionId != null)
                        {
                            playersPerFaction.TryGetValue(factionId, out int playerCount);
                            playersPerFaction[factionId] = playerCount + 1;
                        }
                    }
                }

                UpdateZone(zone, playersPerFaction, scheduleState, 1f);
                CheckPlayers();
                m_Logger.LogInformation(GetCurrentZoneScores(zone)); //this line constantly outputs score of zone
            }
        }

        private void UpdateZone(CaptureZoneRuntime zone, IReadOnlyDictionary<string, int> playersPerFaction, CaptureState scheduleState, float elapsedSeconds)
        {
            if (GetCurrentZoneState() != CaptureState.Scoring)
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
                foreach (SteamPlayer client in Provider.clients)
                {
                    ChatManager.serverSendMessage(
                        "Zones are now availible for Capture",
                        Color.green,
                        null,
                        client,
                        EChatMode.SAY,
                        null,
                        false
                    );
                }
            }

            zone.ScoreTickAccumulator += elapsedSeconds;
            int wholeSeconds = (int)zone.ScoreTickAccumulator;
            if (wholeSeconds > 0)
            {
                zone.ScoreTickAccumulator -= wholeSeconds;
                foreach (KeyValuePair<string, int> faction in playersPerFaction)
                {
                    zone.FactionScores.TryGetValue(faction.Key, out int currentScore);
                    zone.FactionScores[faction.Key] = currentScore + (faction.Value * wholeSeconds);
                }
            }
        }

        private void StartScoringRound(CaptureZoneRuntime zone)
        {
            zone.FactionScores.Clear();
            zone.WinningFactionId = null;
            zone.ScoreTickAccumulator = 0f;
            zone.State = CaptureState.Scoring;
            m_Logger.LogInformation("Scoring started for capture zone {Zone}.", zone.Definition.Name);
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
                    "Capture zone {zone} ended with no winner.", zone.Definition.Name);
                return;
            }

            zone.WinningFactionId = leadingFaction;

            lock (m_stateLock)
            {
                m_FactionRewards.TryGetValue(leadingFaction, out int currentReward);
                m_FactionRewards[leadingFaction] = currentReward + zone.Definition.Weight;
            }

            SaveFactionScoresAsync().Forget();

            m_Logger.LogInformation(
                "{Faction} won {Zone} with {Score} player-seconds and earned {Reward} points for their faction.",
                leadingFaction,
                zone.Definition.Name,
                leadingScore,
                zone.Definition.Weight
            );
        }

        private string? getFactionId(SteamPlayer steamPlayer)
        {
            if (steamPlayer == null || steamPlayer.player == null)
            {
                m_Logger.LogWarning("Invalid steam player.");
                return null;
            }
            ulong steamId = steamPlayer.playerID.steamID.m_SteamID;
            return m_FactionService.GetFactionId(steamId);
        }

        private void CheckPlayers()
        {
            foreach (SteamPlayer steamPlayer in Provider.clients)
            {
                Vector3 position = steamPlayer.player.transform.position;
                CaptureZoneRuntime? currentZone = GetCaptureZoneAt(position.x, position.z);
                ulong steamId = steamPlayer.playerID.steamID.m_SteamID;

                m_PlayerZones.TryGetValue(steamId, out CaptureZoneRuntime? previousZone); 

                if (currentZone != previousZone)
                {
                    if (previousZone != null)
                    {
                        m_Logger.LogInformation("{Player} left {Territory}", steamPlayer.playerID.characterName, previousZone.Definition);
                        ChatManager.serverSendMessage(
                            $"You have left zone {previousZone.Definition.Name}",
                            Color.red,
                            null,
                            steamPlayer,
                            EChatMode.SAY,
                            null,
                            false
                        );
                    }
                    if (currentZone != null)
                    {
                        m_Logger.LogInformation("{Player} entered {Territory}", steamPlayer.playerID.characterName, currentZone.Definition);
                        ChatManager.serverSendMessage(
                            $"You have entered zone {currentZone.Definition.Name}",
                            Color.cyan,
                            null,
                            steamPlayer,
                            EChatMode.SAY,
                            null,
                            false
                        );
                    }
                    m_PlayerZones[steamId] = currentZone;
                }
            }
        }
        
        
    }

}