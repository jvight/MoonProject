using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>
    /// Analytic queries against the generated moon surface, registered in the <see cref="GameContext"/> by the
    /// World domain. Answers come from the height function, not from physics raycasts, so they work for
    /// content placement before colliders exist.
    /// </summary>
    public interface ITerrainQuery
    {
        /// <summary>
        /// World-space XZ rectangle that lies entirely on drivable ground (inscribed in the crater floor). Use
        /// <see cref="IsDrivable"/> for the exact floor shape.
        /// </summary>
        Rect PlayableArea { get; }

        /// <summary>True where the rover can drive comfortably (the basin floor, not the rim or The Peak's flanks).</summary>
        bool IsDrivable(float x, float z);

        /// <summary>Surface height at world XZ.</summary>
        float SampleHeight(float x, float z);

        /// <summary>Surface normal at world XZ.</summary>
        Vector3 SampleNormal(float x, float z);
    }
}
