using System;
using System.Collections.Generic;
using System.Threading;
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
        }
    }
}