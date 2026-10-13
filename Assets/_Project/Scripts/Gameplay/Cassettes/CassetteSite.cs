using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>Where a cassette's container stands: a surface point and the way its open front faces.</summary>
    public readonly struct CassetteSite
    {
        public CassetteSite(Vector3 position, Vector3 facing)
        {
            Position = position;
            Facing = facing;
        }

        /// <summary>On the surface.</summary>
        public Vector3 Position { get; }

        /// <summary>Horizontal unit vector the container's front, and the tape's label, face.</summary>
        public Vector3 Facing { get; }
    }
}
