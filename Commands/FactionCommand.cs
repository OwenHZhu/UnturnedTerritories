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
using System.Collections.Generic;

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
                throw new UserFriendlyException(
                    "Usage: /faction create|remove|delete|list");
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

            if (action == "delete")
            {
                if (Context.Parameters.Length < 2)
                {
                    await player.PrintMessageAsync(
                        "Usage: /faction remove <faction>");

                    return;
                }

                string factionName = Context.Parameters[1];
                bool deleted = m_FactionService.DeleteFaction(factionName);

                if (deleted)
                {
                    await player.PrintMessageAsync($"Faction '{factionName}' has been deleted");
                }
                return;
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
                    throw new UserFriendlyException(
                        "Usage: /faction remove <steamId|playerName> <factionName>"
                    );
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
                        throw new UserFriendlyException(
                            $"Player '{target}' is not connected or could not be found.");
                    }

                    steamId = steamPlayer.playerID.steamID.m_SteamID;
                }

                bool removed = m_FactionService.RemoveFactionId(steamId);

                if (removed)
                {
                    throw new UserFriendlyException(
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

            if (action == "list")
            {
                if (Context.Parameters.Length < 2)
                {
                    throw new UserFriendlyException("Usage: faction list <faction>");
                }

                string factionName = Context.Parameters[1];
                var members = m_FactionService.GetFactionMembers(factionName);

                var existingFaction = m_FactionService.GetFactionByName(factionName) 
                ?? throw new UserFriendlyException("No faction by that name");

                if (members.Count == 0)
                {
                    await player.PrintMessageAsync("No members found in that faction");
                    return;
                }

                var names = members.Select(steamId => Provider.clients.FirstOrDefault(p => p.playerID.steamID.m_SteamID == steamId)?
                .playerID.characterName ?? steamId.ToString()).ToList();

                await player.PrintMessageAsync($"Members of '{factionName}': {string.Join(", ", names)}");

            }
        }
    }
}