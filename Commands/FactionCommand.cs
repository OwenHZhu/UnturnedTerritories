using System.Linq;
using Cysharp.Threading.Tasks;
using OpenMod.API.Commands;
using OpenMod.Core.Commands;
using OpenMod.Unturned.Commands;
using OpenMod.Unturned.Users;
using TerritoryPlugin.Services;
using System;
using OpenMod.Core.Console;
using SDG.Unturned;

namespace TerritoryPlugin.Commands
{
    [Command("faction")]
    [CommandAlias("f")]
    [CommandDescription("Manage factions.")]
    public class FactionCommand : UnturnedCommand
    {
        private readonly IFactionService m_FactionService;

        public FactionCommand(
            IServiceProvider serviceProvider,
            TerritoryService territoryService,
            IFactionService factionService)
            : base(serviceProvider)
        {
            m_FactionService = factionService;
        }

        

        protected override async UniTask OnExecuteAsync()
        {
            var player = (UnturnedUser)Context.Actor;

            if (Context.Parameters.Length == 0)
            {
                await player.PrintMessageAsync(
                    "Usage: /faction create|info|leaderboard");

                return;
            }

            string action = Context.Parameters[0].ToLower();

            if (action == "create")
            {
                if (Context.Parameters.Length < 2)
                {
                    await player.PrintMessageAsync(
                        "Usage: /faction create <name>");

                    return;
                }

                string factionName = Context.Parameters[1];

                var existingFaction = m_FactionService.GetFactionByName(factionName);

                if (existingFaction != null)
                {
                    await player.PrintMessageAsync(
                        $"A faction with the name '{factionName}' already exists.");

                    return;
                }

                var newFaction = m_FactionService.CreateFaction(factionName);

                await player.PrintMessageAsync(
                    $"Faction '{newFaction.Name}' created successfully.");
            }

            if (action == "add")
            {
                if (Context.Parameters.Length < 3)
                {
                    await player.PrintMessageAsync(
                        "Usage: /faction add <factionName> <steamId|playerName>"
                    );
                    return;
                }

                string factionName = Context.Parameters[1];
                string target = Context.Parameters[2];
                ulong steamId;

                if (!ulong.TryParse(target, out steamId))
                {
                    SteamPlayer? steamPlayer = Provider.clients
                        .FirstOrDefault(p => string.Equals(
                            p.playerID.characterName,
                            target,
                            StringComparison.OrdinalIgnoreCase));

                    if (steamPlayer == null)
                    {
                        await player.PrintMessageAsync(
                            $"Player '{target}' is not connected or could not be found.");
                        return;
                    }

                    steamId = steamPlayer.playerID.steamID.m_SteamID;
                }

                m_FactionService.SetFactionId(steamId, factionName);
                await player.PrintMessageAsync(
                    $"Added {target} to faction '{factionName}' as SteamID {steamId}."
                );
            }

            if (action == "remove")
            {
                if (Context.Parameters.Length < 3)
                {
                    await player.PrintMessageAsync(
                        "Usage: /faction remove <steamId|playerName> <factionName>"
                    );
                    return;
                }

                string target = Context.Parameters[1];
                string factionName = Context.Parameters[2];
                ulong steamId;

                if (!ulong.TryParse(target, out steamId))
                {
                    SteamPlayer? steamPlayer = Provider.clients
                        .FirstOrDefault(p => string.Equals(
                            p.playerID.characterName,
                            target,
                            StringComparison.OrdinalIgnoreCase));

                    if (steamPlayer == null)
                    {
                        await player.PrintMessageAsync(
                            $"Player '{target}' is not connected or could not be found.");
                        return;
                    }

                    steamId = steamPlayer.playerID.steamID.m_SteamID;
                }

                bool removed = m_FactionService.RemoveFactionId(steamId);

                if (removed)
                {
                    await player.PrintMessageAsync(
                        $"Removed {target} from faction '{factionName}' as SteamID {steamId}."
                    );
                }
                else
                {
                    await player.PrintMessageAsync(
                        $"No faction membership found for SteamID {steamId}."
                    );
                }
            }
        }
    }
}