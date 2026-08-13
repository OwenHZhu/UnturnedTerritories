using System.Threading.Tasks;
using OpenMod.API.Eventing;
using OpenMod.Unturned.Building;
using OpenMod.Unturned.Building.Events;
using TerritoryPlugin.Services;
using OpenMod.Core.Eventing;

namespace TerritoryPlugin.EventListeners
{
    public class TerritoryDamageListener : IEventListener<UnturnedBuildableDamagingEvent>
    {
        private readonly IFactionService m_FactionService;

        public TerritoryDamageListener(IFactionService factionService)
        {
            m_FactionService = factionService;
        }

        [EventListener]
        public Task HandleEventAsync(object? sender, UnturnedBuildableDamagingEvent @event)
        {
            if (@event.Instigator == null)
            {
                return Task.CompletedTask; // no player instigator (zombie/environment/etc) - allow for now
            }
        
            ulong ownerId = GetOwnerId(@event.Buildable);
        
            if (ownerId == 0)
            {
                return Task.CompletedTask;
            }
        
            string? ownerFaction = m_FactionService.GetFactionId(ownerId);
            string? attackerFaction = m_FactionService.GetFactionId(@event.Instigator.SteamId.m_SteamID);
        
            bool sameFaction =
                ownerFaction != null &&
                attackerFaction != null &&
                string.Equals(ownerFaction, attackerFaction, System.StringComparison.OrdinalIgnoreCase);
        
            if (!sameFaction)
            {
                @event.IsCancelled = true;
            }
        
            return Task.CompletedTask;
        }

        private static ulong GetOwnerId(UnturnedBuildable buildable)
        {
            return buildable switch
            {
                UnturnedBarricadeBuildable barricade => barricade.BarricadeData.owner,
                UnturnedStructureBuildable structure => structure.StructureData.owner,
                _ => 0
            };
        }
    }
}