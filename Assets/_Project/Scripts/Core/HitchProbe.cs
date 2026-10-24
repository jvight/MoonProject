using System.Diagnostics;
using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>Scratch probe: logs any measured span over the threshold to the player log.</summary>
    public static class HitchProbe
    {
        private const double Milliseconds = 4.0;

        public static long Start()
        {
            return Stopwatch.GetTimestamp();
        }

        public static void Stop(string name, long start)
        {
            double ms = (Stopwatch.GetTimestamp() - start) * 1000.0 / Stopwatch.Frequency;
            if (ms >= Milliseconds)
            {
                UnityEngine.Debug.Log($"[HP] {name} {ms:F1} ms at {Time.realtimeSinceStartup:F2}s frame {Time.frameCount}");
            }
        }
    }
}
