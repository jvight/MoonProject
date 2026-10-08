using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>One node of the station's relay network: home (the radio tower) or a relay mast.</summary>
    public readonly struct RelayNode
    {
        public RelayNode(string id, Vector3 position, bool lit, float radius, Vector3 lampPosition)
        {
            Id = id;
            Position = position;
            Lit = lit;
            Radius = radius;
            LampPosition = lampPosition;
        }

        /// <summary>"home" for the radio tower, otherwise the mast's anchor id ("relay.0", ...).</summary>
        public string Id { get; }

        /// <summary>The node's pad on the surface (where a radio-hop arrives).</summary>
        public Vector3 Position { get; }

        /// <summary>True when the node is restored and linked to home, so it extends the station's reach.</summary>
        public bool Lit { get; }

        /// <summary>
        /// Metres of reach the node adds while lit: home's is the radio tower's clear-signal radius at its current
        /// level, a mast's the relay tuning's mast reach.
        /// </summary>
        public float Radius { get; }

        /// <summary>
        /// World position of the node's lamp: a mast's Lamp node standing upright, home's the radio tower's beacon at
        /// the stage the tower shows.
        /// </summary>
        public Vector3 LampPosition { get; }
    }
}
