using MoonProject.Core;

namespace MoonProject.Rover.PlayModeTests
{
    /// <summary><see cref="ICutter"/> stand-in: the starter cutter's charge-up, brace and warm window, with no tier.</summary>
    public sealed class TestCutter : ICutter
    {
        public int Tier => 0;

        public int LampCount => 2;

        public float ChargeSeconds => 0.65f;

        public float ResumeChargeSeconds => 0.3f;

        public float BraceSeconds => 0.5f;

        public float WarmWindowSeconds => 6f;

        public float SpeedFactor => 1f;

        public float DrainFactor => 1f;
    }
}
