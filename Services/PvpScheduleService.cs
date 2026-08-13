using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using Microsoft.Extensions.Logging;
using SDG.Unturned;
using TerritoryPlugin.Models;
using UnityEngine;

namespace TerritoryPlugin.Services
{
    public class PvpScheduleService
    {
        private readonly ILogger<PvpScheduleService> m_Logger;
        private PvpScheduleConfiguration m_Configuration = new PvpScheduleConfiguration();
        private bool m_IsPvpEnabled;

        public bool IsPvpEnabled => m_IsPvpEnabled;

        public PvpScheduleService(ILogger<PvpScheduleService> logger)
        {
            m_Logger = logger;
        }

        public void SetConfiguration(PvpScheduleConfiguration configuration)
        {
            m_Configuration = configuration;
        }

        public async UniTask StartAsync(CancellationToken cancellationToken)
        {
            m_IsPvpEnabled = IsWithinPvpWindow();
            m_Logger.LogInformation("PvP schedule started. Initial state: {State}",
                m_IsPvpEnabled ? "Enabled" : "Disabled");

            while (!cancellationToken.IsCancellationRequested)
            {
                bool shouldBeEnabled = IsWithinPvpWindow();

                if (shouldBeEnabled != m_IsPvpEnabled)
                {
                    m_IsPvpEnabled = shouldBeEnabled;
                    BroadcastStateChange(m_IsPvpEnabled);
                }

                await UniTask.Delay(TimeSpan.FromSeconds(1), cancellationToken: cancellationToken);
            }
        }

        private bool IsWithinPvpWindow()
        {
            TimeSpan currentTime = DateTime.Now.TimeOfDay;
            TimeSpan startTime = TimeSpan.Parse(m_Configuration.EnabledStart);
            TimeSpan endTime = TimeSpan.Parse(m_Configuration.EnabledEnd);

            if (startTime < endTime)
            {
                return currentTime >= startTime && currentTime < endTime;
            }

            return currentTime >= startTime || currentTime < endTime; // overnight window
        }

        private void BroadcastStateChange(bool isEnabled)
        {
            string message = isEnabled
                ? "Grace Period is now over."
                : "PvP is now DISABLED. Players are safe from each other.";

            Color color = isEnabled ? Color.red : Color.green;

            foreach (SteamPlayer client in Provider.clients)
            {
                ChatManager.serverSendMessage(message, color, null, client, EChatMode.SAY, null, false);
            }

            m_Logger.LogInformation("PvP state changed: {State}", isEnabled ? "Enabled" : "Disabled");
        }
    }
}