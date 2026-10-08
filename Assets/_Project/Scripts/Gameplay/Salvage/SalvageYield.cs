using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// What a piece is worth and how long it takes to cut: bigger pieces yield more units (by the volume of their
    /// bounds), drag pieces a fixed generous amount, and each cut lasts a second or so per unit within a short, calm
    /// range. Pure maths, shared by the salvage sites and the economy test (VISION ruling 5).
    /// </summary>
    public static class SalvageYield
    {
        /// <summary>Units a site piece of <paramref name="size"/> (m, its bounds) yields.</summary>
        public static int Units(Vector3 size, bool drag, SalvageTuning tuning)
        {
            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            if (drag)
            {
                return tuning.DragYield;
            }

            float volume = Mathf.Abs(size.x * size.y * size.z);
            return volume < tuning.SmallVolume ? tuning.SmallYield
                : volume < tuning.MediumVolume ? tuning.MediumYield
                : tuning.LargeYield;
        }

        /// <summary>Seconds the beam takes to cut a piece worth <paramref name="units"/>.</summary>
        public static float CutSeconds(int units, SalvageTuning tuning)
        {
            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            Vector2 range = tuning.CutSeconds;
            return Mathf.Clamp(Mathf.Max(0, units) * tuning.CutSecondsPerUnit, range.x, range.y);
        }
    }
}
