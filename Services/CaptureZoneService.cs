using System;
using System.Collections.Generic;
using System.Threading;
using System.Linq;
using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SDG.Unturned;
using TerritoryPlugin.Models;
using UnityEngine;

namespace TerritoryPlugin.Services
{
    public class CaptureZoneService
    {
        private readonly TerritoryService m_TerritoryService;
        private readonly ILogger<CaptureZoneService> m_Logger;

        private readonly Dictionary<ulong, Territory?> m_PlayerTerritories =
            new Dictionary<ulong, Territory?>();

        private readonly List<CaptureZoneRuntime> m_CaptureZones =
            new List<CaptureZoneRuntime>();

        public IReadOnlyList<CaptureZoneRuntime> CaptureZones =>
            m_CaptureZones;

        public CaptureZoneService(
            TerritoryService territoryService,
            ILogger<CaptureZoneService> logger)
        {
            m_TerritoryService = territoryService;
            m_Logger = logger;
        }

        public async UniTask StartAsync(
            CancellationToken cancellationToken)
        {
            m_Logger.LogInformation(
                "CaptureZoneService started.");

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
                "Capture zone {Zone} created at {X:F1}, {Z:F1} with a {Radius:F0}m radius.",
                captureZone.Name,
                captureZone.X,
                captureZone.Z,
                captureZone.Radius);

            return runtime;
        }

        private static string GetFactionId(SteamPlayer player)
        {
            return player.playerID.steamID.m_SteamID.ToString();
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
                var factionsPresent = new HashSet<string>();

                foreach (SteamPlayer steamPlayer in Provider.clients)
                {
                    Vector3 position =
                        steamPlayer.player.transform.position;

                    float dx = position.x - zone.Definition.X;
                    float dz = position.z - zone.Definition.Z;

                    if ((dx * dx) + (dz * dz) <=
                        zone.Definition.Radius * zone.Definition.Radius)
                    {
                        factionsPresent.Add(GetFactionId(steamPlayer));
                    }
                }

                UpdateZone(zone, factionsPresent, 0.5f);
            }
        }

        private void UpdateZone(
            CaptureZoneRuntime zone,
            IReadOnlyCollection<string> factionsPresent,
            float elapsedSeconds)
        {
            if (factionsPresent.Count == 0)
            {
                zone.State = zone.OwnerFactionId == null
                    ? CaptureState.Neutral
                    : CaptureState.Controlled;

                zone.CapturingFactionId = null;
                zone.Progress = 0f;
                return;
            }

            if (factionsPresent.Count > 1)
            {
                if (zone.State != CaptureState.Contested)
                {
                    m_Logger.LogInformation(
                        "Capture zone {Zone} is contested by {FactionCount} players.",
                        zone.Definition.Name,
                        factionsPresent.Count);
                }

                zone.State = CaptureState.Contested;
                return; // Freeze progress while contested.
            }

            string faction = factionsPresent.First();

            if (faction == zone.OwnerFactionId)
            {
                zone.State = CaptureState.Controlled;
                zone.CapturingFactionId = null;
                zone.Progress = 0f;
                return;
            }

            if (zone.CapturingFactionId != faction)
            {
                zone.CapturingFactionId = faction;
                zone.Progress = 0f;

                m_Logger.LogInformation(
                    "{Faction} started capturing {Zone}.",
                    faction,
                    zone.Definition.Name);
            }

            zone.State = CaptureState.Capturing;
            zone.Progress += elapsedSeconds / 30f; // 30 seconds to capture

            if (zone.Progress >= 1f)
            {
                zone.OwnerFactionId = faction;
                zone.CapturingFactionId = null;
                zone.Progress = 0f;
                zone.State = CaptureState.Controlled;

                m_Logger.LogInformation(
                    "{Faction} captured {Zone}.",
                    faction,
                    zone.Definition.Name);
            }
        }
    }
}