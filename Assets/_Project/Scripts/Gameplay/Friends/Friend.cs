using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One friend in the world (owned by <see cref="FriendField"/>): its progress and site, its broken and repaired
    /// rigs, its part pickups, and its flight and behaviour once awake. Its live state is Core's
    /// <see cref="IFriendState"/>, updated by the field every frame.
    /// </summary>
    public sealed class Friend : IFriendState
    {
        internal Friend(FriendDefinition definition, int index, FriendSite site, FriendRig broken, FriendRig repaired,
            Transform[] parts, Transform perch, FriendBehaviour behaviour, RepairSequence sequence)
        {
            Definition = definition;
            Index = index;
            Site = site;
            Progress = new FriendProgress(definition.Id, definition.Parts.Count, definition.Items.Count);
            HomecomingLine = "ticker." + definition.Id + ".home";
            Broken = broken;
            Repaired = repaired;
            Parts = parts;
            Perch = perch;
            Behaviour = behaviour;
            Sequence = sequence;
            int count = parts.Length;
            PartRest = new Vector3[count];
            PartStart = new Vector3[count];
            PartScale = new Vector3[count];
            PartFlightTime = new float[count];
            PartFlightDuration = new float[count];
            PartFlying = new bool[count];
            PartSpotted = new bool[count];
            for (int i = 0; i < count; i++)
            {
                PartRest[i] = parts[i].position;
                PartScale[i] = parts[i].localScale;
            }

            BrokenRotation = broken.Root.rotation;
        }

        public FriendDefinition Definition { get; }

        public string Id => Definition.Id;

        public FriendActivity Activity { get; internal set; }

        public float RotorSpeed { get; internal set; }

        public float RepairProgress { get; internal set; }

        public int Index { get; }

        public FriendSite Site { get; }

        public FriendProgress Progress { get; }

        /// <summary>Its first-homecoming ticker key (when its definition announces one).</summary>
        internal string HomecomingLine { get; }

        /// <summary>Where it is now: its site while broken, its flight once awake.</summary>
        public Vector3 Position => Swapped ? Repaired.Root.position : Site.Position;

        internal FriendRig Broken { get; }

        internal FriendRig Repaired { get; }

        internal Transform[] Parts { get; }

        internal Vector3[] PartRest { get; }

        internal Vector3[] PartStart { get; }

        internal Vector3[] PartScale { get; }

        internal float[] PartFlightTime { get; }

        internal float[] PartFlightDuration { get; }

        internal bool[] PartFlying { get; }

        internal bool[] PartSpotted { get; }

        internal Transform Perch { get; }

        internal FriendBehaviour Behaviour { get; }

        internal FriendMotion Motion { get; } = new FriendMotion();

        internal RepairSequence Sequence { get; }

        internal Quaternion BrokenRotation { get; }

        /// <summary>Game time the repair began (meaningful while Repairing).</summary>
        internal float RepairStart { get; set; }

        /// <summary>The broken rig has been swapped for the repaired one (boot-up under way or done).</summary>
        internal bool Swapped { get; set; }

        internal float RotorLevel { get; set; }

        internal float EyeLevel { get; set; }

        internal float ConeLevel { get; set; }

        /// <summary>Part lamps first, then one lamp per required item.</summary>
        internal float[] LampLevels { get; } = new float[FriendProgress.MaxParts + FriendProgress.MaxItems];
    }
}
