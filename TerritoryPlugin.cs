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
using SDG.Unturned;
using TerritoryPlugin.Models;
using System.Collections.Generic;

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
            
            var section = configuration.GetSection("capture_zones");
            
            var zoneConfig = new CaptureZoneConfiguration
            {
                TimeZoneId = section["time_zone_id"] ?? "America/Santiago",
                ScoringStart = section["scoring_start"] ?? "18:30",
                ScoringEnd = section["scoring_end"] ?? "19:00",
                Zones = section.GetSection("zones").Get<List<CaptureZone>>() ?? new List<CaptureZone>()
            };
            
            m_Logger.LogInformation(
                "Loaded config - TimeZone: {TZ}, Start: {Start}, End: {End}, Zones: {Count}",
                zoneConfig.TimeZoneId, zoneConfig.ScoringStart, zoneConfig.ScoringEnd, zoneConfig.Zones.Count);
            m_Logger.LogInformation(m_Configuration["test:string"]);
            
            m_CaptureZoneService.SetConfiguration(zoneConfig);
        }

        protected override Task OnLoadAsync()
        {
            m_Logger.LogInformation(m_StringLocalizer["plugin_events:plugin_start"]);
            m_CaptureZoneService.StartAsync(CancellationToken.None).Forget();

            return Task.CompletedTask;
        }

        protected override Task OnUnloadAsync()
        {
            m_Logger.LogInformation(m_StringLocalizer["plugin_events:plugin_stop"]);
            return Task.CompletedTask;
        }
    }
}
