using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Where a broken friend lies, which way it faces and where its missing parts are, as planned on the surface.
    /// </summary>
    public sealed class FriendSite
    {
        public FriendSite(Vector3 position, Vector3 normal, Vector3 facing, Vector3[] parts, bool inCrater,
            bool visible)
        {
            Position = position;
            Normal = normal;
            Facing = facing;
            Parts = parts;
            InCrater = inCrater;
            Visible = visible;
        }

        /// <summary>The site floor (on the surface).</summary>
        public Vector3 Position { get; }

        public Vector3 Normal { get; }

        /// <summary>Horizontal unit vector it faces as it lies there (home, or back the way 07 arrives).</summary>
        public Vector3 Facing { get; }

        /// <summary>Surface points of the parts, in part order.</summary>
        public Vector3[] Parts { get; }

        /// <summary>A planned site lies in a hollow of the asked-for depth (false for an anchored site).</summary>
        public bool InCrater { get; }

        /// <summary>A planned site is in clear view from the base edge (false for an anchored site).</summary>
        public bool Visible { get; }
    }
}
