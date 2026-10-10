namespace MoonProject.Gameplay
{
    /// <summary>How a station's pad lamps glow and breathe (from the station's tuning).</summary>
    public readonly struct PadLook
    {
        public PadLook(float radius, float idle, float inviting, float occupied, float breathPeriod, float breathDepth,
            float ease)
        {
            Radius = radius;
            Idle = idle;
            Inviting = inviting;
            Occupied = occupied;
            BreathPeriod = breathPeriod;
            BreathDepth = breathDepth;
            Ease = ease;
        }

        /// <summary>07 counts as parked within this many metres of the centre; Art's nearest pad size is shown.</summary>
        public float Radius { get; }

        /// <summary>Lamp brightness with nothing affordable yet.</summary>
        public float Idle { get; }

        /// <summary>Lamp brightness when what it offers is affordable.</summary>
        public float Inviting { get; }

        /// <summary>Lamp brightness while 07 is parked on it.</summary>
        public float Occupied { get; }

        public float BreathPeriod { get; }

        public float BreathDepth { get; }

        /// <summary>Seconds (time constant) to brighten or dim.</summary>
        public float Ease { get; }
    }
}
