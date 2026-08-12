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
                throw new CommandWrongUsageException("Usage: /zone set|status|info");
            }

            string action = Context.Parameters[0].ToLower();

            if (action == "set")
            {
                if (Context.Parameters.Length < 2)
                {
                    throw new CommandWrongUsageException("Usage: /zone set <name>");
                }

                string zoneName = Context.Parameters[1];
                Vector3 position = player.Player.Player.transform.position;

                var zone = new CaptureZone
                {
                    Name = zoneName,
                    X = position.x,
                    Y = position.y,
                    Z = position.z,
                    Radius = 50f
                };

                m_CaptureZoneService.AddCaptureZone(zone);
                await player.PrintMessageAsync(
                    $"Capture zone '{zone.Name}' created!");

                await player.PrintMessageAsync(
                    $"Center: {zone.X:F1}, {zone.Y:F1}, {zone.Z:F1}");

                await player.PrintMessageAsync(
                    $"Radius: {zone.Radius:F0}m");

                await player.PrintMessageAsync(
                    $"Scoring window: {m_CaptureZoneService.GetCaptureWindow()}");
                return;
            }

            if (action == "status")
            {
                Vector3 position =
                    player.Player.Player.transform.position;

                CaptureZoneRuntime? zoneRuntime =
                    m_CaptureZoneService.GetCaptureZoneAt(
                        position.x,
                        position.z);
                if (zoneRuntime == null)
                {
                    await player.PrintMessageAsync(
                        "You are not inside a capture zone.");
                    return;
                }

                await player.PrintMessageAsync(
                    $"You are inside capture zone '{zoneRuntime.Definition.Name}'.");
                await player.PrintMessageAsync(
                    $"Radius: {zoneRuntime.Definition.Radius:F0}m");
                await player.PrintMessageAsync(
                    $"State: {zoneRuntime.State}");
                await player.PrintMessageAsync(
                    m_CaptureZoneService.GetCurrentZoneScores(zoneRuntime));
                return;
            }
        }
        
    }
}