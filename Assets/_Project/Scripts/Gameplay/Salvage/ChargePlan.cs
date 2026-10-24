namespace MoonProject.Gameplay
{
    /// <summary>
    /// How one cut's charge builds (docs/design/0.5.5-owner-feedback.md §5.1): the brace (skipped by a warm emitter),
    /// then the stages the piece needs, each locking in turn and holding still a moment before the next builds, then the
    /// held breath before the beam fires.
    /// </summary>
    public readonly struct ChargePlan
    {
        public ChargePlan(float braceSeconds, float stageSeconds, float holdSeconds, int stages, float breathSeconds,
            bool warm, float startFraction)
        {
            HoldSeconds = holdSeconds;
            StartFraction = startFraction;
            BraceSeconds = warm ? 0f : braceSeconds;
            StageSeconds = stageSeconds;
            Stages = stages < 1 ? 1 : stages;
            BreathSeconds = breathSeconds;
            Warm = warm;
        }

        /// <summary>Seconds 07 plants itself before anything builds (0 when warm).</summary>
        public float BraceSeconds { get; }

        /// <summary>Seconds each stage takes to build.</summary>
        public float StageSeconds { get; }

        /// <summary>Seconds a locked stage holds still before the next one builds (none after the last).</summary>
        public float HoldSeconds { get; }

        /// <summary>Stages the piece needs: small 1, medium 2, large and hull 3.</summary>
        public int Stages { get; }

        /// <summary>Seconds of stillness after the full lock, before ignition.</summary>
        public float BreathSeconds { get; }

        /// <summary>A re-press soon after the last firing at the same site: no brace, quicker stages.</summary>
        public bool Warm { get; }

        /// <summary>
        /// Where the charge starts, 0..1 of a fresh one (the Anticipate's strength): 0 fresh, part-way when warm.
        /// </summary>
        public float StartFraction { get; }

        /// <summary>The charge in all, press to ignition.</summary>
        public float TotalSeconds => BraceSeconds + Stages * StageSeconds + (Stages - 1) * HoldSeconds + BreathSeconds;

        /// <summary>When stage <paramref name="stage"/> (1..<see cref="Stages"/>) locks, from the press.</summary>
        public float LockAt(int stage)
        {
            return BraceSeconds + stage * StageSeconds + (stage - 1) * HoldSeconds;
        }
    }
}
