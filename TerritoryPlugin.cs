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
        private readonly PvpScheduleService m_PvpScheduleService;
        private readonly ZoneEffectService m_CaptureZoneEffectService;

        private readonly CancellationTokenSource m_LifetimeCts = new CancellationTokenSource();

        public TerritoryPlugin(
            IConfiguration configuration, 
            IStringLocalizer stringLocalizer,
            ILogger<TerritoryPlugin> logger, 
            CaptureZoneService captureZoneService,
            PvpScheduleService pvpScheduleService,
            ZoneEffectService captureZoneEffectService,
            IServiceProvider serviceProvider) : base(serviceProvider)
        {
            m_Configuration = configuration;
            m_StringLocalizer = stringLocalizer;
            m_Logger = logger;
            m_CaptureZoneService = captureZoneService;
            m_PvpScheduleService = pvpScheduleService;
            m_CaptureZoneEffectService = captureZoneEffectService;
            
            var section = configuration.GetSection("capture_zones");

            //debugging
            //
            //
            m_Logger.LogInformation(
                "DEBUG ring_effect_id raw value = '{Value}'",
                section["ring_effect_id"] ?? "NULL");
            //
            //
            //

            ushort ringEffectId = ushort.TryParse(section["ring_effect_id"], out var parsedEffectId)
                ? parsedEffectId
                : (ushort)0;
            float ringRefreshIntervalSeconds = float.TryParse(section["ring_refresh_interval_seconds"], out var parsedInterval)
                ? parsedInterval
                : 4f;

            var zoneConfig = new CaptureZoneConfiguration
            {
                TimeZoneId = section["time_zone_id"] ?? "America/Santiago",
                ScoringStart = section["scoring_start"] ?? "18:30",
                ScoringEnd = section["scoring_end"] ?? "19:00",
                Zones = section.GetSection("zones").Get<List<CaptureZone>>() ?? new List<CaptureZone>(),
                RingEffectId = ringEffectId,
                RingRefreshIntervalSeconds = ringRefreshIntervalSeconds
            };

            var pvpSection = configuration.GetSection("pvp_schedule");
            var pvpConfig = new PvpScheduleConfiguration
            {
                EnabledStart = pvpSection["enabled_start"] ?? "00:00",
                EnabledEnd = pvpSection["enabled_end"] ?? "23:59"
            };
            
            m_Logger.LogInformation(
                "Loaded config - TimeZone: {TZ}, Start: {Start}, End: {End}, Zones: {Count}, RingEffectId: {EffectId}",
                zoneConfig.TimeZoneId, zoneConfig.ScoringStart, zoneConfig.ScoringEnd, zoneConfig.Zones.Count, zoneConfig.RingEffectId);
            
            m_CaptureZoneService.SetConfiguration(zoneConfig);
            m_PvpScheduleService.SetConfiguration(pvpConfig);
            m_CaptureZoneEffectService.Configure(zoneConfig.RingEffectId, zoneConfig.RingRefreshIntervalSeconds);
        }

        protected override Task OnLoadAsync()
        {
            m_Logger.LogInformation(m_StringLocalizer["plugin_events:plugin_start"]);
            m_Logger.LogInformation("TerritoryPlugin Onloaded");

            m_CaptureZoneService.StartAsync(m_LifetimeCts.Token).Forget();
            m_PvpScheduleService.StartAsync(m_LifetimeCts.Token).Forget();
m_CaptureZoneEffectService
    .StartAsync(m_LifetimeCts.Token)
    .Forget(ex =>
    {
        m_Logger.LogError(
            ex,
            "Zone effect service crashed.");
    });

            return Task.CompletedTask;
        }

        protected override Task OnUnloadAsync()
        {
            m_Logger.LogInformation(m_StringLocalizer["plugin_events:plugin_stop"]);
            m_LifetimeCts.Cancel();
            return Task.CompletedTask;
        }
    }
}