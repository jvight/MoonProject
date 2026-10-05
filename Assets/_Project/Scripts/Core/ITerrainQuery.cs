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
        /// <summary>World-space XZ rectangle the player can drive in (inside the crater rim).</summary>
        Rect PlayableArea { get; }

        /// <summary>Surface height at world XZ.</summary>
        float SampleHeight(float x, float z);

        /// <summary>Surface normal at world XZ.</summary>
        Vector3 SampleNormal(float x, float z);
    }
}
