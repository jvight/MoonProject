using System;
using MoonProject.Core;

namespace MoonProject.Audio
{
    /// <summary>
    /// Which sounds each salvage material makes (M3-13): Metal a deeper grind and a crack with a hollow clunk, Wiring
    /// a crackle and a snap, Optics a glassy shimmer and a glassy crack.
    /// </summary>
    public static class SalvageSounds
    {
        /// <summary>The cutting texture loop of <paramref name="material"/>.</summary>
        public static string CutCue(SalvageMaterial material)
        {
            switch (material)
            {
                case SalvageMaterial.Metal:
                    return AudioCueIds.SalvageCutMetal;
                case SalvageMaterial.Wiring:
                    return AudioCueIds.SalvageCutWiring;
                case SalvageMaterial.Optics:
                    return AudioCueIds.SalvageCutOptics;
                default:
                    throw new ArgumentOutOfRangeException(nameof(material), material, "Unknown salvage material.");
            }
        }

        /// <summary>The <see cref="AudioCueIds.SalvageBreak"/> variant label of <paramref name="material"/>.</summary>
        public static string BreakLabel(SalvageMaterial material)
        {
            switch (material)
            {
                case SalvageMaterial.Metal:
                    return "metal";
                case SalvageMaterial.Wiring:
                    return "wiring";
                case SalvageMaterial.Optics:
                    return "optics";
                default:
                    throw new ArgumentOutOfRangeException(nameof(material), material, "Unknown salvage material.");
            }
        }
    }
}
