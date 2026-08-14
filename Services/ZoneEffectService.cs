using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SDG.Unturned;
using TerritoryPlugin.Models;
using UnityEngine;

namespace TerritoryPlugin.Services
{
    public class ZoneEffectService
    {
        private const int RingPointCount = 48;
        private const float GroundRaycastHeight = 1000f;
        private const float GroundRaycastDistance = 2000f;

        private ushort m_RingEffectId;

        private readonly CaptureZoneService m_CaptureZoneService;
        private readonly ILogger<ZoneEffectService> m_Logger;

        private readonly Dictionary<string, List<Vector3>> m_RingPointCache = new Dictionary<string, List<Vector3>>(StringComparer.OrdinalIgnoreCase);

        private EffectAsset? m_RingEffectAsset;
        private float m_RefreshIntervalSeconds = 1.5f;

        public ZoneEffectService(
            CaptureZoneService captureZoneService,
            ILogger<ZoneEffectService> logger)
        {
            m_CaptureZoneService = captureZoneService;
            m_Logger = logger;
        }

        public void Configure(
            ushort ringEffectId,
            float refreshIntervalSeconds)
        {
            m_RingEffectId = ringEffectId;
            m_RefreshIntervalSeconds = refreshIntervalSeconds;

            m_Logger.LogInformation(
                "Zone effect service configured: EffectId={EffectId}, Refresh={Refresh}s",
                ringEffectId,
                refreshIntervalSeconds);
        }

        public async UniTask StartAsync(CancellationToken cancellationToken)
        {
            m_Logger.LogInformation("Starting zone effect service.");

            await UniTask.SwitchToMainThread();

            // Wait for Unturned's asset system to become available.
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    m_RingEffectAsset =
                        Assets.find(EAssetType.EFFECT, m_RingEffectId) as EffectAsset;

                    if (m_RingEffectAsset != null)
                    {
                        break;
                    }

                    m_Logger.LogWarning("Ring effect {EffectId} is not available yet. Retrying...", m_RingEffectId);
                }
                catch (NullReferenceException)
                {
                    m_Logger.LogDebug("Unturned asset system is not ready yet. Retrying...");
                }

                await UniTask.Delay(
                    TimeSpan.FromSeconds(1),
                    cancellationToken: cancellationToken);

                await UniTask.SwitchToMainThread();
            }

            if (cancellationToken.IsCancellationRequested)
            {
                return;
            }

            m_Logger.LogInformation("Loaded ring effect: ID={EffectId}, Name={Name}", m_RingEffectId, m_RingEffectAsset!.name);

            while (!cancellationToken.IsCancellationRequested)
            {
                await RefreshZoneRingsAsync();

                await UniTask.Delay(
                    TimeSpan.FromSeconds(m_RefreshIntervalSeconds),
                    cancellationToken: cancellationToken);
            }
        }

        private async UniTask RefreshZoneRingsAsync()
        {
            if (m_RingEffectAsset == null)
            {
                return;
            }

            await UniTask.SwitchToMainThread();

            foreach (CaptureZoneRuntime zone in m_CaptureZoneService.CaptureZonesList)
            {
                List<Vector3> ringPoints = GetOrBuildRingPoints(zone);

                foreach (Vector3 point in ringPoints)
                {
                    var parameters = new TriggerEffectParameters(m_RingEffectAsset)
                    {
                        position = point,
                        relevantDistance = zone.Definition.Radius + 900f
                    };

                    EffectManager.triggerEffect(parameters);
                }
            }
        }

        private List<Vector3> GetOrBuildRingPoints(CaptureZoneRuntime zone)
        {
            string zoneName = zone.Definition.Name;

            if (m_RingPointCache.TryGetValue(zoneName, out List<Vector3>? cached))
            {
                return cached;
            }

            var points = new List<Vector3>(RingPointCount);
            float radius = zone.Definition.Radius;
            float centerX = zone.Definition.X;
            float centerZ = zone.Definition.Z;

            for (int i = 0; i < RingPointCount; i++)
            {
                float angle = i / (float)RingPointCount * Mathf.PI * 2f;
                float pointX = centerX + Mathf.Cos(angle) * radius;
                float pointZ = centerZ + Mathf.Sin(angle) * radius;
                float pointY = zone.Definition.Y + 0.1f;

                points.Add(new Vector3(pointX, pointY, pointZ));
                m_Logger.LogInformation("Ring point {Index}: X={X}, Y={Y}, Z={Z}",i,pointX,pointY,pointZ);
            }

            m_RingPointCache[zoneName] = points;
            return points;
        }

        private float ResolveGroundHeight(float x, float z)
        {
            Vector3 origin = new Vector3(x, GroundRaycastHeight, z);

            if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, GroundRaycastDistance, RayMasks.GROUND))
            {

                return hit.point.y;
            }

            m_Logger.LogWarning("Could not resolve ground height at ({X}, {Z}); defaulting to 0.", x, z);
            return 0f;
        }

        public void InvalidateCache(string zoneName)
        {
            m_RingPointCache.Remove(zoneName);
        }
    }
}