using System;
using Cysharp.Threading.Tasks;
using OpenMod.API.Commands;
using OpenMod.Core.Commands;
using OpenMod.Unturned.Commands;
using SDG.Unturned;
using UnityEngine;

namespace TerritoryPlugin.Commands
{
    [Command("effectat")]
    [CommandDescription("Spawns an effect at given coordinates")]
    [CommandSyntax("<effectId> <x> <y> <z>")]
    public class CommandEffectAt : UnturnedCommand
    {
        public CommandEffectAt(IServiceProvider serviceProvider) : base(serviceProvider) { }

        protected override async UniTask OnExecuteAsync()
        {
            if (Context.Parameters.Length != 4)
                throw new CommandWrongUsageException(Context);

            ushort effectId = await Context.Parameters.GetAsync<ushort>(0);
            float x = await Context.Parameters.GetAsync<float>(1);
            float y = await Context.Parameters.GetAsync<float>(2);
            float z = await Context.Parameters.GetAsync<float>(3);

            // Everything below touches Unturned/Unity APIs, so make sure
            // we're back on the game thread before calling any of it.
            await UniTask.SwitchToMainThread();

            var effectAsset = Assets.find(EAssetType.EFFECT, effectId) as EffectAsset;
            if (effectAsset == null)
            {
                await PrintAsync($"No effect asset with ID {effectId}");
                return;
            }

            var parameters = new TriggerEffectParameters(effectAsset)
            {
                position = new Vector3(x, y, z),
                relevantDistance = EffectManager.MEDIUM
            };

            EffectManager.triggerEffect(parameters);
        }
    }
}