using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One friend in the world (owned by <see cref="FriendField"/>): its progress and site, its part pickups, its home
    /// socket and its body (rigs, repair beat, awake life). Its live state is Core's <see cref="IFriendState"/>,
    /// updated by the field every frame.
    /// </summary>
    public sealed class Friend : IFriendState
    {
        internal Friend(FriendDefinition definition, int index, FriendSite site, FriendProgress progress,
            Transform[] parts, Transform home, IFriendBody body)
        {
            Definition = definition;
            Index = index;
            Site = site;
            Progress = progress;
            Parts = parts;
            Home = home;
            Body = body;
            HomecomingLine = "ticker." + definition.Id + ".home";
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
        }

        public FriendDefinition Definition { get; }

        public string Id => Definition.Id;

        public FriendActivity Activity { get; internal set; }

        public float RotorSpeed { get; internal set; }

        public float RepairProgress { get; internal set; }

        public int Index { get; }

        public FriendSite Site { get; }

        public FriendProgress Progress { get; }

        /// <summary>Where it is now: its site while broken, its body once it is up.</summary>
        public Vector3 Position => Body.Position;

        /// <summary>It lives at the base right now.</summary>
        public bool IsHome => Progress.State == FriendState.Awake && Body.IsHome;

        /// <summary>Its first-homecoming ticker key (when its definition announces one).</summary>
        internal string HomecomingLine { get; }

        internal IFriendBody Body { get; }

        /// <summary>Its home socket at the base (FriendSocket_tilly on the lander, BellCorner by the tower).</summary>
        internal Transform Home { get; }

        internal Transform[] Parts { get; }

        internal Vector3[] PartRest { get; }

        internal Vector3[] PartStart { get; }

        internal Vector3[] PartScale { get; }

        internal float[] PartFlightTime { get; }

        internal float[] PartFlightDuration { get; }

        internal bool[] PartFlying { get; }

        internal bool[] PartSpotted { get; }

        /// <summary>Game time the repair began (meaningful while Repairing).</summary>
        internal float RepairStart { get; set; }

        /// <summary>Part lamps first, then one lamp per required item.</summary>
        internal float[] LampLevels { get; } = new float[FriendProgress.MaxParts + FriendProgress.MaxItems];
    }
}
