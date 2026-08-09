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
    [Command("zone")]
    [CommandAlias("z")]
    [CommandDescription("Manage capture zones.")]
    public class ZoneCommand : UnturnedCommand
    {
        private readonly CaptureZoneService m_CaptureZoneService;
        public ZoneCommand(
            IServiceProvider serviceProvider,
            CaptureZoneService captureZoneService)
            : base(serviceProvider)
        {
            m_CaptureZoneService = captureZoneService;
        }
        
        protected override async UniTask OnExecuteAsync()
        {
            var player = (UnturnedUser)Context.Actor;
            
            if (Context.Parameters.Length == 0)
            {
                throw new CommandWrongUsageException("Usage: /zone add|remove|info");
            }

            string action = Context.Parameters[0].ToLower();

            if (action == "set")
            {
                Vector3 position =
                    player.Player.Player.transform.position;
                var zone = new CaptureZone
                {
                    Name = $"Zone {m_CaptureZoneService.CaptureZones.Count + 1}",
                    X = position.x,
                    Y = position.y,
                    Z = position.z,
                    Radius = 50f
                };
                m_CaptureZoneService.AddCaptureZone(zone);
                await player.PrintMessageAsync(
                    "Capture zone created!");

                await player.PrintMessageAsync(
                    $"Center: {zone.X:F1}, {zone.Y:F1}, {zone.Z:F1}");

                await player.PrintMessageAsync(
                    $"Radius: {zone.Radius:F0}m");
            }

            if (action == "leaderboard")
            {
                var leaderboard = m_CaptureZoneService.GetFactionLeaderboard();
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
                    m_CaptureZoneService.CurrentScheduleState ==
                    CaptureState.Scoring
                        ? $"{zone.Definition.Name}: " +
                          $"{m_CaptureZoneService.GetRemainingSeconds(zone):F0}s remaining"
                        : $"{zone.Definition.Name} is not currently scoring " +
                          $"({m_CaptureZoneService.CurrentScheduleState}).");

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



        }
        
    }
}