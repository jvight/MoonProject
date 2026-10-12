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

        /// <summary>The front of home a friend checks on (the charging dock), and the way she faces it from.</summary>
        public Vector3 Inspect { get; set; }

        public Vector3 InspectForward { get; set; }

        /// <summary>07 crawls home on its reserve and this friend leads it (docs/features/M3-20).</summary>
        public bool Lead { get; set; }

        /// <summary>The charger it leads 07 toward (valid while <see cref="Lead"/>).</summary>
        public Vector3 LeadToward { get; set; }

        /// <summary>It fetches a waiting bundle home (docs/features/M3-25 T3); leading 07 comes first.</summary>
        public bool Fetch { get; set; }

        /// <summary>Where it flies on its fetch: the bundle, then home's shelves (valid while <see cref="Fetch"/>).</summary>
        public Vector3 FetchToward { get; set; }

        /// <summary>Its calm fetching speed (m/s).</summary>
        public float FetchSpeed { get; set; }

        /// <summary>Metres over the ground it flies while fetching.</summary>
        public float FetchHeight { get; set; }
    }
}
