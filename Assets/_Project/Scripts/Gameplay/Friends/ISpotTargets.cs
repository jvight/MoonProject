using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>What a spotter friend can look for (implemented over relics, friend parts and scrap).</summary>
    public interface ISpotTargets
    {
        /// <summary>
        /// The most interesting thing not yet spotted within <paramref name="radius"/> of <paramref name="around"/>:
        /// undiscovered relics first, then friend parts, then scrap clusters (nearest within a kind).
        /// </summary>
        bool TryFind(Vector3 around, float radius, out SpotTarget target);

        /// <summary>Records <paramref name="target"/> as spotted (a relic is revealed on 07's sonar).</summary>
        void MarkSpotted(SpotTarget target);
    }
}
