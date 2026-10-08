using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// One shaped patch of ground: a flat disc at <see cref="Plateau"/> height out to <see cref="Radius"/>, blending
    /// back into the ground around it over <see cref="Run"/> metres along an eased straight ramp (C1), which keeps
    /// the blend's steepest pitch low for its length.
    /// </summary>
    public readonly struct GroundShape
    {
        // Share of the run eased at each end of the blend.
        private const float EaseShare = 0.25f;

        public GroundShape(Vector2 center, float radius, float run, float plateau)
        {
            Center = center;
            Radius = radius;
            Run = run;
            Plateau = plateau;
        }

        /// <summary>World XZ of the centre.</summary>
        public Vector2 Center { get; }

        /// <summary>Radius of the flat disc, metres.</summary>
        public float Radius { get; }

        /// <summary>Width of the blend back into the ground, metres.</summary>
        public float Run { get; }

        /// <summary>Height of the flat disc.</summary>
        public float Plateau { get; }

        /// <summary>
        /// How much the shape holds the ground <paramref name="distance"/> from its centre: 1 on the disc.
        /// </summary>
        public float Weight(float distance)
        {
            return 1f - SmoothMath.SmoothRamp(distance - Radius, Run, Run * EaseShare);
        }
    }
}
