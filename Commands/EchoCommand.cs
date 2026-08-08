using System;
using Cysharp.Threading.Tasks;
using OpenMod.API.Commands;
using OpenMod.Core.Commands;
using OpenMod.Unturned.Commands;
using OpenMod.Unturned.Users;

namespace TerritoryPlugin.Commands
{
    [Command("echo")]
    public class EchoCommand : UnturnedCommand
    {
        public EchoCommand(IServiceProvider serviceProvider)
            : base(serviceProvider)
        {
        }

        protected override async UniTask OnExecuteAsync()
        {
            var player = (UnturnedUser)Context.Actor;

            await player.PrintMessageAsync("Hello from TerritoryPlugin!");
        }
    }
}