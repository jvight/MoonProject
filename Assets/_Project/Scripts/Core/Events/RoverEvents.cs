using UnityEngine;

namespace MoonProject.Core.Events
{
    /// <summary>The rover touched down after being airborne. Drives landing audio, dust puffs and camera bump.</summary>
    public readonly struct RoverLanded
    {
        public RoverLanded(Vector3 position, float impactSpeed, float airTime)
        {
            Position = position;
            ImpactSpeed = impactSpeed;
            AirTime = airTime;
        }

        public Vector3 Position { get; }

        /// <summary>Downward speed at touchdown in m/s (always positive).</summary>
        public float ImpactSpeed { get; }

        /// <summary>Seconds spent airborne before this landing.</summary>
        public float AirTime { get; }
    }

    /// <summary>
    /// 07 began waking up at the start of the session (eye opening, design ruling 8): the moment the radio crackles on.
    /// </summary>
    public readonly struct RoverAwoke
    {
        public RoverAwoke(Vector3 position, bool wokenByPlayer)
        {
            Position = position;
            WokenByPlayer = wokenByPlayer;
        }

        public Vector3 Position { get; }

        /// <summary>True when the player drove before 07 woke on its own (the wake-up is quick, not slow).</summary>
        public bool WokenByPlayer { get; }
    }
}
