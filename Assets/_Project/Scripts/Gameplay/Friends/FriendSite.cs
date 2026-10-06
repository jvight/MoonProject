using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>Where a broken friend lies and where its missing parts are, as planned on the surface.</summary>
    public sealed class FriendSite
    {
        public FriendSite(Vector3 position, Vector3 normal, Vector3[] parts, bool inCrater, bool visible)
        {
            Position = position;
            Normal = normal;
            Parts = parts;
            InCrater = inCrater;
            Visible = visible;
        }

        /// <summary>The site floor (on the surface).</summary>
        public Vector3 Position { get; }

        public Vector3 Normal { get; }

        /// <summary>Surface points of the parts, in part order.</summary>
        public Vector3[] Parts { get; }

        /// <summary>The site lies in a hollow of the asked-for depth.</summary>
        public bool InCrater { get; }

        /// <summary>The site is in clear view from the base edge.</summary>
        public bool Visible { get; }
    }
}
