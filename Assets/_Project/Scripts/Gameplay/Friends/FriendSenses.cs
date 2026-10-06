using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>What an awake friend knows about the world this frame.</summary>
    public struct FriendSenses
    {
        public float Now { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Rover { get; set; }
        public Vector3 RoverForward { get; set; }
        public Vector3 Camera { get; set; }

        /// <summary>The lander (home).</summary>
        public Vector3 Home { get; set; }

        /// <summary>Its perch on the lander.</summary>
        public Vector3 Perch { get; set; }

        /// <summary>Which way the perch faces.</summary>
        public Vector3 PerchForward { get; set; }

        /// <summary>The museum shelf and the way its front faces.</summary>
        public Vector3 Shelf { get; set; }

        public Vector3 ShelfForward { get; set; }
    }
}
