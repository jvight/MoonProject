using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// Whether 07 notices its elder "06" now, as plain logic (docs/design/0.5.5-owner-feedback.md §1.7): once ever,
    /// the first time it comes within the notice radius (flat distance); a saved game that has met 06 never looks
    /// again.
    /// </summary>
    public sealed class ElderGlance
    {
        private readonly ElderGlanceSettings _settings;

        public ElderGlance(ElderGlanceSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>True once 07 has looked at 06 (saved).</summary>
        public bool Greeted { get; private set; }

        /// <summary>A loaded game's memory of the meeting.</summary>
        public void Restore(bool greeted)
        {
            Greeted = greeted;
        }

        /// <returns>True on the one step 07 first comes near 06.</returns>
        public bool Step(Vector3 rover, Vector3 elder)
        {
            if (Greeted)
            {
                return false;
            }

            float dx = rover.x - elder.x;
            float dz = rover.z - elder.z;
            float radius = _settings.NoticeRadius;
            Greeted = dx * dx + dz * dz <= radius * radius;
            return Greeted;
        }
    }
}
