using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The lander's blast-swept zone on the ground (<see cref="BlastZoneSettings"/>): how swept a point is and how
    /// much of the swept rock lies piled round the edge there. The terrain shader draws the sweep itself
    /// (<see cref="BlastZoneShaderGlobals"/>). Immutable and thread-safe.
    /// </summary>
    public sealed class BlastZone
    {
        public BlastZone(Vector2 center, BlastZoneSettings settings)
        {
            Settings = settings ?? throw new ArgumentNullException(nameof(settings));
            Center = center;
        }

        /// <summary>The lander's centre on the ground (world x, z).</summary>
        public Vector2 Center { get; }

        public BlastZoneSettings Settings { get; }

        /// <summary>1 where the blast swept the ground bare, easing to 0 across the edge band.</summary>
        public float Swept(float x, float z)
        {
            float radius = Settings.Radius;
            return 1f - SmoothMath.Smootherstep(radius - Settings.EdgeBand, radius, Distance(x, z));
        }

        /// <summary>1 on the pile of swept pebbles just outside the edge, easing to 0 either side of it.</summary>
        public float EdgePile(float x, float z)
        {
            float halfWidth = Settings.EdgePileWidth * 0.5f;
            return SmoothMath.Bump((Distance(x, z) - Settings.Radius - halfWidth) / halfWidth);
        }

        /// <summary>
        /// What the zone does to the chance of a rock at (x, z): none where it is swept, extra pebbles (only when
        /// <paramref name="pebble"/>) on the pile round its edge.
        /// </summary>
        public float ScatterChance(float x, float z, float chance, bool pebble)
        {
            float kept = chance * (1f - Swept(x, z));
            return pebble ? kept + Settings.EdgePebbles * EdgePile(x, z) : kept;
        }

        private float Distance(float x, float z)
        {
            float dx = x - Center.x;
            float dz = z - Center.y;
            return Mathf.Sqrt(dx * dx + dz * dz);
        }
    }
}
