namespace MoonProject.Audio
{
    /// <summary>One cue a wake beat plays: which, its variant, how long after the beat, and how loud.</summary>
    public readonly struct WakeCueCall
    {
        public WakeCueCall(WakeCueKind kind, int variant, float delay, float volume)
        {
            Kind = kind;
            Variant = variant;
            Delay = delay;
            Volume = volume;
        }

        public WakeCueKind Kind { get; }

        public int Variant { get; }

        /// <summary>Seconds after the beat.</summary>
        public float Delay { get; }

        /// <summary>Share of the cue's own volume.</summary>
        public float Volume { get; }
    }
}
