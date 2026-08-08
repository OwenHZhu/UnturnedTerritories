using System.Collections.Generic;

namespace TerritoryPlugin.Models
{
    public enum CaptureState
    {
        Scoring,
        Finished
    }

    public class CaptureZoneRuntime
    {
        public CaptureZoneRuntime(CaptureZone definition)
        {
            Definition = definition;
        }

        public CaptureZone Definition { get; }

        public CaptureState State { get; set; } = CaptureState.Scoring;

        public float ElapsedSeconds { get; set; }

        public float ScoreTickAccumulator { get; set; }

        public Dictionary<string, int> FactionScores { get; } =
            new Dictionary<string, int>();

        public string? WinningFactionId { get; set; }
    }
}
