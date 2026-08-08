using System;
using Cysharp.Threading.Tasks;
using OpenMod.API.Commands;
using OpenMod.Core.Commands;
using OpenMod.Unturned.Commands;
using OpenMod.Unturned.Users;
using TerritoryPlugin.Models;
using UnityEngine;

namespace TerritoryPlugin.Commands
{
    [Command("territory")]
    public class TerritoryCommand : UnturnedCommand
    {
        public TerritoryCommand(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
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

                var territory = new Territory
                {
                    Name = $"Territory at {position.x:F0}, {position.z:F0}",
                    X = position.x,
                    Y = position.y,
                    Z = position.z,
                    Radius = 100f
                };

                await player.PrintMessageAsync(
                    $"You claimed {territory.Name}!");

                await player.PrintMessageAsync(
                    $"Center: {territory.X:F1}, {territory.Y:F1}, {territory.Z:F1}");

                await player.PrintMessageAsync(
                    $"Radius: {territory.Radius:F0}m");

                return;
            }

            await player.PrintMessageAsync(
                $"Unknown territory command: {action}");
        }
    }
}