using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>What an awake friend wants to do this frame (from <see cref="FriendBehaviour"/>).</summary>
    public struct FriendIntent
    {
        public Vector3 Target { get; set; }
        public Vector3 Look { get; set; }
        public float Speed { get; set; }

        /// <summary>0 still .. 1 flying.</summary>
        public float Rotors { get; set; }

        /// <summary>Eye glow (1 awake, lower while napping).</summary>
        public float Eye { get; set; }

        /// <summary>0..1 its little spotting light.</summary>
        public float Cone { get; set; }

        /// <summary>0..1 how visible it is (fades out and back in when it reappears behind 07).</summary>
        public float Visibility { get; set; }

        /// <summary>Jump straight to <see cref="Target"/> this frame (while invisible).</summary>
        public bool Teleport { get; set; }

        /// <summary>It began greeting 07 this frame.</summary>
        public bool Greeted { get; set; }

        /// <summary>It began pinging softly over <see cref="Spot"/> this frame.</summary>
        public bool Spotted { get; set; }

        public SpotTarget Spot { get; set; }
    }
}
