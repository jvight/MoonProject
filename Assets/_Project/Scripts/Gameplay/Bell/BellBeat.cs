using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>One frame of Bell's life (see <see cref="BellLife"/>): her pose and what she did.</summary>
    public readonly struct BellBeat
    {
        public BellBeat(BellPose pose, FriendActivity activity, float motor, bool greeted, bool footTapped)
        {
            Pose = pose;
            Activity = activity;
            Motor = motor;
            Greeted = greeted;
            FootTapped = footTapped;
        }

        public BellPose Pose { get; }

        public FriendActivity Activity { get; }

        /// <summary>0..1 leg effort (Core's IFriendState.RotorSpeed): dancing, waddling.</summary>
        public float Motor { get; }

        /// <summary>She began greeting 07 this frame (the jingle).</summary>
        public bool Greeted { get; }

        /// <summary>She began tapping a foot to the music this frame.</summary>
        public bool FootTapped { get; }
    }
}
