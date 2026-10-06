using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>A planned scrap piece: where it floats, which variant it is and its idle-motion offsets.</summary>
    public readonly struct ScrapSpawn
    {
        public ScrapSpawn(Vector3 position, int variant, float phase, float yaw)
        {
            Position = position;
            Variant = variant;
            Phase = phase;
            Yaw = yaw;
        }

        /// <summary>Rest position (already lifted to the hover height above the surface).</summary>
        public Vector3 Position { get; }

        /// <summary>Index into <see cref="ScrapCatalog.Variants"/>.</summary>
        public int Variant { get; }

        /// <summary>Bob phase in radians, so neighbours never move in lockstep.</summary>
        public float Phase { get; }

        /// <summary>Initial heading in degrees.</summary>
        public float Yaw { get; }
    }
}
