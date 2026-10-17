using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>The weld's energy cost as the whole percent the build menu says (pure, EditMode-tested).</summary>
    internal static class WeldEnergyShare
    {
        public const int MaxPercent = 100;

        /// <summary>
        /// The whole percent of a full battery for a share of 0..1: at least 1 for any cost at all, so a weld never
        /// reads as free.
        /// </summary>
        public static int Percent(float share01)
        {
            if (share01 <= 0f)
            {
                return 0;
            }

            return Mathf.Clamp(Mathf.RoundToInt(share01 * MaxPercent), 1, MaxPercent);
        }
    }
}
