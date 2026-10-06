using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// A round depression in the floor: flat-ish bottom, smooth walls and an optional raised rim. Seeded impact
    /// craters and hand-placed play bowls share this shape.
    /// </summary>
    public readonly struct Crater
    {
        public Crater(Vector2 center, float radius, float depth, float rimHeight, float rimWidth, bool isPlayBowl)
        {
            Center = center;
            Radius = radius;
            Depth = depth;
            RimHeight = rimHeight;
            RimWidth = rimWidth;
            IsPlayBowl = isPlayBowl;
        }

        /// <summary>World XZ of the centre.</summary>
        public Vector2 Center { get; }

        /// <summary>Radius where the wall meets the rim crest, metres.</summary>
        public float Radius { get; }

        /// <summary>Depth of the bottom below the surrounding ground, metres.</summary>
        public float Depth { get; }

        /// <summary>Height of the raised rim crest, metres.</summary>
        public float RimHeight { get; }

        /// <summary>Half width of the rim as a fraction of <see cref="Radius"/>.</summary>
        public float RimWidth { get; }

        /// <summary>True for hand-placed play bowls, false for seeded impact craters.</summary>
        public bool IsPlayBowl { get; }

        /// <summary>Radius beyond which the crater no longer changes the surface.</summary>
        public float OuterRadius => Radius * (1f + RimWidth);
    }
}
