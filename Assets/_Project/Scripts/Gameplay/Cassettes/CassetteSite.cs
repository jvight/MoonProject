using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>Where a cassette waits: a surface point, the way its label faces, and whether it is tucked.</summary>
    public readonly struct CassetteSite
    {
        public CassetteSite(Vector3 position, Vector3 facing, bool tucked)
        {
            Position = position;
            Facing = facing;
            Tucked = tucked;
        }

        /// <summary>On the surface.</summary>
        public Vector3 Position { get; }

        /// <summary>Horizontal unit vector the label faces (away from the rim or wall it leans on).</summary>
        public Vector3 Facing { get; }

        /// <summary>It leans against a small crater rim (a basin spot), or stands at its anchor.</summary>
        public bool Tucked { get; }
    }
}
