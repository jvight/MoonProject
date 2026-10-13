using MoonProject.Core;

namespace MoonProject.Audio.PlayModeTests
{
    /// <summary>The cutter for audio tests: a tier and its lamps that a test sets.</summary>
    public sealed class FakeCutter : ICutter
    {
        public int Tier { get; set; }

        public int LampCount { get; set; } = 5;

        public float ChargeSeconds { get; set; } = 0.65f;

        public float ResumeChargeSeconds { get; set; } = 0.3f;

        public float BraceSeconds => 0.5f;

        public float WarmWindowSeconds => 6f;

        public float SpeedFactor => 1f;

        public float DrainFactor => 1f;
    }
}
