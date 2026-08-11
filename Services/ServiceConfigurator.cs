using Microsoft.Extensions.DependencyInjection;
using OpenMod.API.Ioc;

namespace TerritoryPlugin.Services
{
    public class ServiceConfigurator : IServiceConfigurator
    {
        public void ConfigureServices(
            IOpenModServiceConfigurationContext openModStartupContext,
            IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton<TerritoryService>();
            serviceCollection.AddSingleton<CaptureZoneService>();
            serviceCollection.AddSingleton<IFactionService, FactionService>();
        }
    }
}