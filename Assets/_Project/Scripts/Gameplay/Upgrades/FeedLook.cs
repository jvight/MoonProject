namespace MoonProject.Gameplay
{
    /// <summary>How 07 feeds a station's hopper (from the station's tuning): see <see cref="HopperFeed"/>.</summary>
    public readonly struct FeedLook
    {
        public FeedLook(float beamLead, float flight, float stagger, float lift, float growShare)
        {
            BeamLead = beamLead;
            Flight = flight;
            Stagger = stagger;
            Lift = lift;
            GrowShare = growShare;
        }

        /// <summary>Seconds the beam reaches the hopper before the first bundle leaves 07.</summary>
        public float BeamLead { get; }

        /// <summary>Seconds each bundle takes from 07's cargo socket into the hopper's mouth.</summary>
        public float Flight { get; }

        /// <summary>Seconds between one bundle leaving and the next.</summary>
        public float Stagger { get; }

        /// <summary>Metres a bundle's flight bows up above the straight line.</summary>
        public float Lift { get; }

        /// <summary>Share of a flight spent popping out of the socket, and again shrinking into the mouth.</summary>
        public float GrowShare { get; }
    }
}
