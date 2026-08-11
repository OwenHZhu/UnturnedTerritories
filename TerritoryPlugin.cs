using System;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using Microsoft.Extensions.Logging;
using OpenMod.Core.Plugins;
using OpenMod.API.Plugins;
using TerritoryPlugin.Services;

// For more, visit https://openmod.github.io/openmod-docs/devdoc/guides/getting-started.html

[assembly: PluginMetadata("TerritoryPlugin", DisplayName = "Territory Plugin")]
namespace TerritoryPlugin
{
    public class TerritoryPlugin : OpenModUniversalPlugin
    {
        private readonly IConfiguration m_Configuration;
        private readonly IStringLocalizer m_StringLocalizer;
        private readonly ILogger<TerritoryPlugin> m_Logger;
        private readonly CaptureZoneService m_CaptureZoneService;

        public TerritoryPlugin(
            IConfiguration configuration, 
            IStringLocalizer stringLocalizer,
            ILogger<TerritoryPlugin> logger, 
            CaptureZoneService captureZoneService,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            m_Configuration = configuration;
            m_StringLocalizer = stringLocalizer;
            m_Logger = logger;
            m_CaptureZoneService = captureZoneService;
        }


        protected override Task OnUnloadAsync()
        {
            m_Logger.LogInformation(m_StringLocalizer["plugin_events:plugin_stop"]);
            return Task.CompletedTask;
        }
    }
}
