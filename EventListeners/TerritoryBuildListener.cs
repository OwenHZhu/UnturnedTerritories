using System.Threading.Tasks;
using OpenMod.API.Eventing;
using OpenMod.API.Users;
using OpenMod.Core.Eventing;
using OpenMod.Core.Users;
using OpenMod.Unturned.Building.Events;
using TerritoryPlugin.Services;

namespace TerritoryPlugin.EventListeners
{
    public class TerritoryBuildListener : IEventListener<UnturnedBuildableDeployingEvent>
    {
        private readonly TerritoryService m_TerritoryService;
        private readonly IUserManager m_UserManager;

        public TerritoryBuildListener(
            TerritoryService territoryService,
            IUserManager userManager)
        {
            m_TerritoryService = territoryService;
            m_UserManager = userManager;
        }

        [EventListener]
        public async Task HandleEventAsync(object? sender, UnturnedBuildableDeployingEvent @event)
        {
            bool isAllowed = m_TerritoryService.CanPlayerBuild(
                @event.Owner,
                @event.Point.x,
                @event.Point.z);

            if (isAllowed)
            {
                return;
            }

            @event.IsCancelled = true;

            var user = await m_UserManager.FindUserAsync(
                KnownActorTypes.Player,
                @event.Owner.ToString(),
                UserSearchMode.FindById);

            if (user is OpenMod.Unturned.Users.UnturnedUser unturnedUser)
            {
                await unturnedUser.PrintMessageAsync(
                    "You can only build within your own faction's territory.");
            }
        }
    }
}