using System;
using Cysharp.Threading.Tasks;
using OpenMod.API.Commands;
using OpenMod.Core.Commands;
using OpenMod.Unturned.Commands;
using OpenMod.Unturned.Users;
using TerritoryPlugin.Models;
using TerritoryPlugin.Services;
using UnityEngine;

namespace TerritoryPlugin.Commands
{
    [Command("territory")]
    [CommandAlias("t")]
    [CommandDescription("Manage territories.")]
    public class TerritoryCommand : UnturnedCommand
    {
        private readonly TerritoryService m_TerritoryService;
        private readonly CaptureZoneService m_CaptureZoneService;

        public TerritoryCommand(
            IServiceProvider serviceProvider,
            TerritoryService territoryService,
            CaptureZoneService captureZoneService)
            : base(serviceProvider)
        {
            m_TerritoryService = territoryService;
            m_CaptureZoneService = captureZoneService;
        }

        protected override async UniTask OnExecuteAsync()
        {
            var player = (UnturnedUser)Context.Actor;

            if (Context.Parameters.Length == 0)
            {
                await player.PrintMessageAsync(
                    "Usage: /territory claim|info|status|leaderboard");

                return;
            }

            string action = Context.Parameters[0].ToLower();

            if (action == "leaderboard")
            {
                var leaderboard =
                    m_CaptureZoneService.GetFactionLeaderboard();

                if (leaderboard.Count == 0)
                {
                    await player.PrintMessageAsync(
                        "No faction points have been awarded yet.");

                    return;
                }

                await player.PrintMessageAsync("Faction leaderboard:");

                int rank = 1;

                foreach (var faction in leaderboard)
                {
                    await player.PrintMessageAsync(
                        $"{rank}. Group {faction.Key}: {faction.Value} points");

                    rank++;
                }

                return;
            }

            if (action == "status")
            {
                Vector3 position =
                    player.Player.Player.transform.position;

                CaptureZoneRuntime? zone =
                    m_CaptureZoneService.GetCaptureZoneAt(
                        position.x,
                        position.z);

                if (zone == null)
                {
                    await player.PrintMessageAsync(
                        "You are not inside a capture zone.");

                    return;
                }

                await player.PrintMessageAsync(
                    $"{zone.Definition.Name}: " +
                    $"{m_CaptureZoneService.GetRemainingSeconds(zone):F0}s remaining");

                var leaderboard =
                    m_CaptureZoneService.GetZoneLeaderboard(zone, 3);

                if (leaderboard.Count == 0)
                {
                    await player.PrintMessageAsync(
                        "No group has scored in this zone yet.");

                    return;
                }

                await player.PrintMessageAsync("Zone leaderboard:");

                int rank = 1;

                foreach (var faction in leaderboard)
                {
                    await player.PrintMessageAsync(
                        $"{rank}. Group {faction.Key}: {faction.Value} score");

                    rank++;
                }

                return;
            }

            //territory claim
            if (action == "claim")
            {
                Vector3 position =
                    player.Player.Player.transform.position;

                Territory? existingTerritory =
                    m_TerritoryService.GetTerritoryAt(
                        position.x,
                        position.z);

                if (existingTerritory != null)
                {
                    await player.PrintMessageAsync(
                        "This location is already inside a territory.");

                    return;
                }

                var territory = new Territory
                {
                    Name = $"Territory {m_TerritoryService.Territories.Count + 1}",
                    X = position.x,
                    Y = position.y,
                    Z = position.z,
                    Radius = 100f
                };

                m_TerritoryService.AddTerritory(territory);

                m_CaptureZoneService.AddCaptureZone(new CaptureZone
                {
                    Name = territory.Name,
                    X = territory.X,
                    Y = territory.Y,
                    Z = territory.Z,
                    Radius = territory.Radius
                });

                await player.PrintMessageAsync(
                    "Territory and test capture zone created!");

                await player.PrintMessageAsync(
                    $"Center: {territory.X:F1}, {territory.Y:F1}, {territory.Z:F1}");

                await player.PrintMessageAsync(
                    $"Radius: {territory.Radius:F0}m");

                return;
            }

            //territory info
            if (action == "info")
            {
                Vector3 position =
                    player.Player.Player.transform.position;

                Territory? territory =
                    m_TerritoryService.GetTerritoryAt(
                        position.x,
                        position.z);

                if (territory == null)
                {
                    await player.PrintMessageAsync(
                        "You are not inside a territory.");

                    return;
                }

                float dx = position.x - territory.X;
                float dz = position.z - territory.Z;

                float distance = Mathf.Sqrt(dx * dx + dz * dz);

                await player.PrintMessageAsync(
                    $"Territory: {territory.Name}");

                await player.PrintMessageAsync(
                    $"Center: {territory.X:F1}, {territory.Y:F1}, {territory.Z:F1}");

                await player.PrintMessageAsync(
                    $"Radius: {territory.Radius:F0}m");
                
                await player.PrintMessageAsync(
                    $"Distance to center: {distance:F1}m");

                return;
            }

            await player.PrintMessageAsync(
                "Unknown subcommand. Use /territory claim|info|status|leaderboard");
        }
    }
}
