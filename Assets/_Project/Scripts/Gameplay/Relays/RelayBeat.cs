using System;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// One mast's restoration in seconds from its start (docs/features/M3-06): 07's beam stitches while the relay part
    /// glides along it into the part socket, then the mast straightens up (rocking once past upright, with a creak),
    /// then its lamp warms. Pure.
    /// </summary>
    public sealed class RelayBeat
    {
        private readonly float _stitch;
        private readonly float _partSlide;
        private readonly float _straighten;
        private readonly float _overshoot;
        private readonly float _warm;

        public RelayBeat(float stitch, float partSlide, float straighten, float overshoot, float warm)
        {
            if (stitch <= 0f || partSlide <= 0f || straighten <= 0f || warm <= 0f)
            {
                throw new ArgumentException("Every stage of a restoration takes time.");
            }

            _stitch = stitch;
            _partSlide = Math.Min(partSlide, stitch);
            _straighten = straighten;
            _overshoot = overshoot;
            _warm = warm;
        }

        public static RelayBeat For(RelayTuning tuning)
        {
            if (tuning == null)
            {
                throw new ArgumentNullException(nameof(tuning));
            }

            return new RelayBeat(tuning.Stitch, tuning.PartSlide, tuning.Straighten, tuning.StraightenOvershoot,
                tuning.LampWarm);
        }

        /// <summary>The beam stops and the mast starts to straighten (s).</summary>
        public float StitchEnd => _stitch;

        /// <summary>The part clicks into its socket (s).</summary>
        public float PartIn => _partSlide;

        /// <summary>The mast stands upright and the lamp starts to warm (s).</summary>
        public float StraightenEnd => _stitch + _straighten;

        /// <summary>The whole restoration (s): at its end the lamp is warm.</summary>
        public float Duration => StraightenEnd + _warm;

        public bool Beaming(float t)
        {
            return t < _stitch;
        }

        /// <summary>0..1 along the part's glide from 07 into the socket.</summary>
        public float Part(float t)
        {
            return Ease.InOutSine(t / _partSlide);
        }

        /// <summary>0 leaning (broken) .. 1 upright, past 1 while it rocks past upright.</summary>
        public float Upright(float t)
        {
            return t <= _stitch ? 0f : Ease.OutBack((t - _stitch) / _straighten, _overshoot);
        }

        /// <summary>0..1 through the lamp's warm-up.</summary>
        public float Warm(float t)
        {
            return t <= StraightenEnd ? 0f : Ease.InOutSine((t - StraightenEnd) / _warm);
        }

        public bool Done(float t)
        {
            return t >= Duration;
        }
    }
}
