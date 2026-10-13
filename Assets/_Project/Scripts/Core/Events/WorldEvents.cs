using UnityEngine;

namespace MoonProject.Core.Events
{
    /// <summary>
    /// One step of home's windows lit as the base wakes (polish wave 1): published by World's home lighting once per
    /// step, <see cref="Step"/> 1..<see cref="Steps"/>, never when a saved stage is restored at once. Audio plays a
    /// soft warm click on each, rising.
    /// </summary>
    public readonly struct HomeWindowLit
    {
        public HomeWindowLit(int step, int steps)
        {
            Step = step;
            Steps = steps;
        }

        /// <summary>The step just lit, 1-based.</summary>
        public int Step { get; }

        /// <summary>How many steps the windows wake in.</summary>
        public int Steps { get; }
    }

    /// <summary>
    /// The network's handshake reached a node (north-star §4a beat 7): when a mast lights, World's network pulse runs
    /// lamp to lamp out from it, and publishes this as each lamp flashes, starting with the new mast (hop 0). Audio
    /// can step up the pentatonic with each hop; home lights its cables when the pulse reaches it.
    /// </summary>
    public readonly struct NetworkPulseReached
    {
        public NetworkPulseReached(string nodeId, Vector3 position, int hop, int hops)
        {
            NodeId = nodeId;
            Position = position;
            Hop = hop;
            Hops = hops;
        }

        /// <summary>The node's id: "home" for the radio tower, otherwise the mast's ("relay.0", ...).</summary>
        public string NodeId { get; }

        /// <summary>World position of the node's lamp, where it flashes.</summary>
        public Vector3 Position { get; }

        /// <summary>Hops from the newly lit mast: 0 is the mast itself.</summary>
        public int Hop { get; }

        /// <summary>Hops to the farthest node this handshake reaches.</summary>
        public int Hops { get; }
    }
}
