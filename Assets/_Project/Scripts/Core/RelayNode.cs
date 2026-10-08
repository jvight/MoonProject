using UnityEngine;

namespace MoonProject.Core
{
    /// <summary>One node of the station's relay network: home (the radio tower) or a relay mast.</summary>
    public readonly struct RelayNode
    {
        public RelayNode(string id, Vector3 position, bool lit)
        {
            Id = id;
            Position = position;
            Lit = lit;
        }

        /// <summary>"home" for the radio tower, otherwise the mast's anchor id ("relay.0", ...).</summary>
        public string Id { get; }

        /// <summary>The node's pad on the surface (where a radio-hop arrives).</summary>
        public Vector3 Position { get; }

        /// <summary>True when the node is restored and linked to home, so it extends the station's reach.</summary>
        public bool Lit { get; }
    }
}
