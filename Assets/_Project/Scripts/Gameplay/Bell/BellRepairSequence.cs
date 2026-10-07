using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Bell's repair as a timeline from its start (docs/features/M3-05 beat 4; pure curves): 07's beam stitches her
    /// while she shivers; then 07 slides the tape in along the beam; her dial lamp flickers and catches; the needle
    /// sweeps across the band and back to Lumen After Dark; and she stands up on her four legs. At its end she is
    /// awake (her jingle and two-step follow). Once begun it always plays to the end.
    /// </summary>
    public sealed class BellRepairSequence
    {
        // A flicker pattern (on/off steps per second), irregular so it reads as an old valve catching.
        private const float FlickerSteps = 8f;
        private const float FlickerOn = 0.6f;

        /// <summary>How far the flicker's "on" share runs ahead of the progress (it holds before the end).</summary>
        private const float FlickerLead = 0.2f;

        /// <summary>The needle starts its sweep halfway through the dial's warming.</summary>
        private const float SweepAfterWarm = 0.5f;

        private readonly float _stitch;
        private readonly float _tape;
        private readonly float _warm;
        private readonly float _sweep;
        private readonly float _stand;

        public BellRepairSequence(float stitch, float tapeSlide, float dialWarm, float needleSweep, float standUp)
        {
            _stitch = Mathf.Max(0.01f, stitch);
            _tape = Mathf.Max(0.01f, tapeSlide);
            _warm = Mathf.Max(0.01f, dialWarm);
            _sweep = Mathf.Max(0.01f, needleSweep);
            _stand = Mathf.Max(0.01f, standUp);
        }

        public static BellRepairSequence For(FriendDefinition definition, BellTuning tuning)
        {
            return new BellRepairSequence(definition.RepairDuration, tuning.TapeSlide, tuning.DialWarm,
                tuning.NeedleSweep, tuning.StandUp);
        }

        /// <summary>When the stitching ends and the tape sets off along the beam (s).</summary>
        public float StitchEnd => _stitch;

        /// <summary>When the tape clicks into her slot (s): the beam lets go.</summary>
        public float TapeIn => _stitch + _tape;

        /// <summary>When the needle starts to sweep (s).</summary>
        public float SweepStart => TapeIn + _warm * SweepAfterWarm;

        /// <summary>When she starts to stand up (s).</summary>
        public float StandStart => Mathf.Max(TapeIn + _warm, SweepStart + _sweep);

        /// <summary>The whole sequence (s): at its end she is up.</summary>
        public float Duration => StandStart + _stand;

        public bool Stitching(float t)
        {
            return t >= 0f && t < _stitch;
        }

        /// <summary>07 holds still and its beam reaches her: the stitching, then the tape's glide.</summary>
        public bool Beaming(float t)
        {
            return t >= 0f && t < TapeIn;
        }

        /// <summary>0..1 through the stitching.</summary>
        public float StitchProgress(float t)
        {
            return Mathf.Clamp01(t / _stitch);
        }

        /// <summary>0..1 of the tape's glide from 07 into her slot (eased).</summary>
        public float Tape(float t)
        {
            return Ease.InOutSine((t - _stitch) / _tape);
        }

        /// <summary>0 dark .. 1 glowing: flickers once the tape is in, then holds.</summary>
        public float DialLamp(float t)
        {
            float warm = t - TapeIn;
            if (warm < 0f)
            {
                return 0f;
            }

            if (warm >= _warm)
            {
                return 1f;
            }

            float progress = warm / _warm;
            float step = Mathf.Floor(warm * FlickerSteps);
            bool on = Mathf.Repeat(step * FlickerOn + progress, 1f) < progress + FlickerLead;
            return on ? Ease.OutCubic(progress) : 0f;
        }

        /// <summary>0..1 of the needle's band: across to the far end and back to the start.</summary>
        public float Needle(float t)
        {
            float p = Mathf.Clamp01((t - SweepStart) / _sweep);
            return Ease.Hump(p);
        }

        /// <summary>0 lying broken .. 1 standing at rest.</summary>
        public float Stand(float t)
        {
            return Ease.InOutSine((t - StandStart) / _stand);
        }

        public bool Done(float t)
        {
            return t >= Duration;
        }
    }
}
