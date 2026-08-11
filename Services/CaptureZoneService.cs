using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using TerritoryPlugin.Models;

namespace TerritoryPlugin.Services
{
    public class CaptureZoneService
    {
        private readonly CaptureZoneConfiguration m_Configuration;
        private readonly List<CaptureZoneRuntime> CaptureZones = new List<CaptureZoneRuntime>();

        public IReadOnlyList<CaptureZoneRuntime> CaptureZonesList => CaptureZones;


        public CaptureZoneService(IConfiguration configuration)
        {
            m_Configuration = new CaptureZoneConfiguration();
            configuration.Bind(m_Configuration);
        }

        public void AddCaptureZone(CaptureZone zone)
        {
            var runtime = new CaptureZoneRuntime(zone);
            CaptureZones.Add(runtime);
        }

        public CaptureZoneRuntime? GetCaptureZoneAt(float x, float z)
        {
            foreach (var runtime in CaptureZones)
            {
                var zone = runtime.Definition;
                float dx = x - zone.X;
                float dz = z - zone.Z;

                float distanceSquared = (dx * dx) + (dz * dz);
                float radiusSquared = zone.Radius * zone.Radius;

                if (distanceSquared <= radiusSquared)
                {
                    return runtime;
                }
            }

            return null;
        }

        public TimeSpan GetCaptureWindow()
        {
            Console.WriteLine($"[TerritoryPlugin DEBUG] scoring_start='{m_Configuration.ScoringStart}' scoring_end='{m_Configuration.ScoringEnd}'");

            TimeSpan startTime = TimeSpan.Parse(m_Configuration.ScoringStart);
            TimeSpan endTime = TimeSpan.Parse(m_Configuration.ScoringEnd);
            return endTime - startTime;
        }
    }
}