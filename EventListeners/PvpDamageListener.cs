using System.Threading.Tasks;
using OpenMod.API.Eventing;
using OpenMod.Unturned.Players.Life.Events;
using OpenMod.Core.Eventing;

using TerritoryPlugin.Services;

namespace TerritoryPlugin.EventListeners
{
    public class PvpDamageListener : IEventListener<UnturnedPlayerDamagingEvent>
    {
        private readonly PvpScheduleService m_PvpScheduleService;

        public PvpDamageListener(PvpScheduleService pvpScheduleService)
        {
            m_PvpScheduleService = pvpScheduleService;
        }

        [EventListener]
        public Task HandleEventAsync(object? sender, UnturnedPlayerDamagingEvent @event)
        {
            if (m_PvpScheduleService.IsPvpEnabled)
            {
                return Task.CompletedTask;
            }

            bool isPlayerVsPlayer =
                @event.Killer != default &&
                @event.Killer.m_SteamID != @event.Player.SteamId.m_SteamID;

            if (isPlayerVsPlayer)
            {
                @event.IsCancelled = true;
            }

            return Task.CompletedTask;
        }
    }
}