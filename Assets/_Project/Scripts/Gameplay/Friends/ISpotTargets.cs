using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>What a spotter friend can look for (implemented over salvage sites and friend parts).</summary>
    public interface ISpotTargets
    {
        /// <summary>
        /// The most interesting thing not yet spotted within <paramref name="radius"/> of <paramref name="around"/>:
        /// undiscovered salvage sites first, then friend parts (nearest within a kind).
        /// </summary>
        bool TryFind(Vector3 around, float radius, out SpotTarget target);

        /// <summary>Records <paramref name="target"/> as spotted (a site is revealed on 07's sonar).</summary>
        void MarkSpotted(SpotTarget target);
    }
}
