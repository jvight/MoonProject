using System;

namespace MoonProject.Audio
{
    /// <summary>
    /// The clunk of each bundle 07's beam drops into a hopper (M3-14): every later bundle of a feed lands a touch
    /// higher, on the ones already in the bin, with a small random wobble, so no two drops of a feed sound alike (the
    /// clunk's variants alternate on top). Allocation-free.
    /// </summary>
    public sealed class BundleDrops
    {
        private readonly StationAudioTuning _tuning;
        private readonly AudioRandom _random;

        public BundleDrops(StationAudioTuning tuning, AudioRandom random)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
            _random = random ?? throw new ArgumentNullException(nameof(random));
        }

        /// <summary>Bundles dropped since the bin was last emptied.</summary>
        public int Dropped { get; private set; }

        /// <summary>A feed starts or ends: the next bundle lands in an empty bin again.</summary>
        public void Restart()
        {
            Dropped = 0;
        }

        /// <summary>The pitch scale of the next bundle's clunk.</summary>
        public float NextPitch()
        {
            float wobble = _tuning.DropPitchWobble * (2f * _random.NextFloat() - 1f);
            float pitch = 1f + Dropped * _tuning.DropPitchStep + wobble;
            Dropped++;
            return pitch;
        }
    }
}
