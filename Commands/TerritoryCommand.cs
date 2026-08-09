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

        public TerritoryCommand(
            IServiceProvider serviceProvider,
            TerritoryService territoryService,
            CaptureZoneService captureZoneService)
            : base(serviceProvider)
        {
            m_TerritoryService = territoryService;
        }

        protected override async UniTask OnExecuteAsync()
        {
            var player = (UnturnedUser)Context.Actor;

            if (Context.Parameters.Length == 0)
            {
                await player.PrintMessageAsync(
                    "Usage: /territory claim|info|leaderboard");

                return;
            }

            string action = Context.Parameters[0].ToLower();

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
                    Radius = 50f
                };

                m_TerritoryService.AddTerritory(territory);

                await player.PrintMessageAsync(
                    "Territory created!");

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
                "Unknown subcommand. Use /territory claim|info|leaderboard");
        }
    }
}
