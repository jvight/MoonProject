using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>What Bell knows about the world this frame (see <see cref="BellLife"/>).</summary>
    public struct BellSenses
    {
        public float Now { get; set; }
        public float DeltaTime { get; set; }

        /// <summary>Where she stands (on the surface).</summary>
        public Vector3 Position { get; set; }

        /// <summary>Her corner at the base.</summary>
        public Vector3 Home { get; set; }

        /// <summary>Bearing (degrees from +Z toward +X) her corner faces.</summary>
        public float HomeYaw { get; set; }

        /// <summary>She has made it home (walked in or set down unseen).</summary>
        public bool AtHome { get; set; }

        /// <summary>Metres she walked this frame on her way home.</summary>
        public float Walked { get; set; }

        public Vector3 Rover { get; set; }

        /// <summary>07's horizontal speed (m/s).</summary>
        public float RoverSpeed { get; set; }

        /// <summary>What the radio plays (the needle's detent; music or quiet).</summary>
        public RadioChannel Channel { get; set; }

        /// <summary>She has greeted 07 coming home before (her first homecoming is behind her).</summary>
        public bool Welcomed { get; set; }
    }
}
