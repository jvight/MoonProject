namespace MoonProject.Gameplay
{
    /// <summary>How a station's pad of light looks and breathes (from the station's tuning).</summary>
    public readonly struct PadLook
    {
        public PadLook(float radius, float ringWidth, int segments, float idle, float inviting, float occupied,
            float done, float breathPeriod, float breathDepth, float ease)
        {
            Radius = radius;
            RingWidth = ringWidth;
            Segments = segments;
            Idle = idle;
            Inviting = inviting;
            Occupied = occupied;
            Done = done;
            BreathPeriod = breathPeriod;
            BreathDepth = breathDepth;
            Ease = ease;
        }

        /// <summary>07 counts as parked within this many metres of the centre.</summary>
        public float Radius { get; }

        public float RingWidth { get; }

        public int Segments { get; }

        /// <summary>Brightness with nothing to buy yet.</summary>
        public float Idle { get; }

        /// <summary>Brightness when what it offers is affordable.</summary>
        public float Inviting { get; }

        /// <summary>Brightness while 07 is parked on it.</summary>
        public float Occupied { get; }

        /// <summary>Brightness once everything it sells is bought.</summary>
        public float Done { get; }

        public float BreathPeriod { get; }

        public float BreathDepth { get; }

        /// <summary>Seconds (time constant) to brighten or dim.</summary>
        public float Ease { get; }
    }
}
