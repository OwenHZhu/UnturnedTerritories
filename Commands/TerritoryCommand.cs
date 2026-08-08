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
            TerritoryService territoryService)
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
                    "Usage: /territory claim");

                return;
            }

            string action = Context.Parameters[0].ToLower();

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

                await player.PrintMessageAsync(
                    $"Territory claimed!");

                await player.PrintMessageAsync(
                    $"Center: {territory.X:F1}, {territory.Y:F1}, {territory.Z:F1}");

                await player.PrintMessageAsync(
                    $"Radius: {territory.Radius:F0}m");

                return;
            }

            await player.PrintMessageAsync(
                "Unknown subcommand. Use /territory claim");
        }
    }
}