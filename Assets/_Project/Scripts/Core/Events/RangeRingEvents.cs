using UnityEngine;

namespace MoonProject.Core.Events
{
    /// <summary>
    /// The range ring grew because a node of 07's network became a charge point (opening-v3-payoff-audit §2: relay.1
    /// fixed). Gameplay draws the old ring reaching out to the new one on the ground; UI and Audio may mark the moment.
    /// Reaches are how far the ring stands from 07 on average round it, before and after.
    /// </summary>
    public readonly struct RingGrew
    {
        public RingGrew(string nodeId, Vector3 node, float oldReach, float newReach)
        {
            NodeId = nodeId;
            Node = node;
            OldReach = oldReach;
            NewReach = newReach;
        }

        /// <summary>The node that became a charge point (relay.1).</summary>
        public string NodeId { get; }

        /// <summary>Where it stands: its contact pad.</summary>
        public Vector3 Node { get; }

        public float OldReach { get; }

        public float NewReach { get; }
    }

    /// <summary>
    /// The range ring was shown (<see cref="IRangeRing"/>): faint at the first charge, in full at the release. UI may
    /// draw it on 07's face for <see cref="Seconds"/>; Audio may soften the moment.
    /// </summary>
    public readonly struct RangeRingShown
    {
        public RangeRingShown(RangeRingMode mode, float seconds)
        {
            Mode = mode;
            Seconds = seconds;
        }

        public RangeRingMode Mode { get; }

        /// <summary>Seconds the ring stays on show.</summary>
        public float Seconds { get; }
    }
}
