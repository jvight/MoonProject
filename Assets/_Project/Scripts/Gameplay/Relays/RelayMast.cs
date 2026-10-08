using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One relay mast's live state (<see cref="RelayField"/>): its broken and restored rigs on its anchor, its relay
    /// part, what was paid, its restoration's progress, whether its lamp shows it linked home, and its link pulse.
    /// </summary>
    internal sealed class RelayMast
    {
        public RelayMast(int node, WorldAnchor anchor, RelayRig broken, RelayRig restored, Transform part,
            Vector3 partRest, LinkPulse pulse)
        {
            Node = node;
            Anchor = anchor;
            Broken = broken ?? throw new ArgumentNullException(nameof(broken));
            Restored = restored ?? throw new ArgumentNullException(nameof(restored));
            Part = part != null ? part : throw new ArgumentNullException(nameof(part));
            PartRest = partRest;
            PartScale = part.localScale;
            Pulse = pulse ?? throw new ArgumentNullException(nameof(pulse));
        }

        /// <summary>Its index in the station's reach (home is 0).</summary>
        public int Node { get; }

        public WorldAnchor Anchor { get; }

        public string Id => Anchor.Id;

        public RelayRig Broken { get; }

        public RelayRig Restored { get; }

        public Transform Part { get; }

        /// <summary>Where the part rests on the ground (on the surface).</summary>
        public Vector3 PartRest { get; }

        public Vector3 PartScale { get; }

        public LinkPulse Pulse { get; }

        public RelayPartState PartState { get; set; }

        public Vector3 FlightStart { get; set; }

        public float FlightTime { get; set; }

        public float FlightDuration { get; set; }

        /// <summary>Material units paid for its restoration (0 until one began).</summary>
        public int Paid { get; set; }

        /// <summary>A restoration began (it counts as restored from then on, in the save too).</summary>
        public bool IsRestored { get; set; }

        /// <summary>Its restoration beat is playing.</summary>
        public bool Restoring { get; set; }

        public float RestoreStart { get; set; }

        /// <summary>The beat swapped the broken rig for the restored one.</summary>
        public bool Swapped { get; set; }

        /// <summary>The beat stood it upright and joined it to the station's reach.</summary>
        public bool Joined { get; set; }

        /// <summary>Its lamp shows it linked home (full glow) rather than listening (low glow).</summary>
        public bool ShownLit { get; set; }

        /// <summary>When it comes online after a neighbour linked it (seconds; infinity when not scheduled).</summary>
        public float OnlineAt { get; set; } = float.PositiveInfinity;

        public float LampFrom { get; set; }

        public float LampTo { get; set; }

        public float LampStart { get; set; } = float.NegativeInfinity;

        /// <summary>The rig on show.</summary>
        public RelayRig Shown => Swapped ? Restored : Broken;
    }
}
