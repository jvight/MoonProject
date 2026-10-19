using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Rover
{
    /// <summary>The parts of 07 whose share of the thirty-six years of regolith comes off on its own.</summary>
    public enum DustZone
    {
        Head = 0,
        Wing = 1,
        Antenna = 2,
        Turret = 3,
        Body = 4,
        Wheels = 5,
    }

    /// <summary>
    /// How much of the long-sleep regolith coat is still on each part of 07 during its wake, as a pure model
    /// (EditMode-tested): the whole coat is on from the title until the checks; each part sheds most of it while its
    /// own check moves it (a part whose check is not run keeps it all); the rest settles off in the ready performance
    /// as the headlamps come back on. It also counts what has fallen, so the opening can drop a puff of dust at the
    /// part each time enough has gone.
    /// </summary>
    public sealed class PostDust
    {
        private readonly PostSettings _settings;
        private readonly float[] _levels = new float[ZoneCount];
        private readonly float[] _fallen = new float[ZoneCount];

        public PostDust(PostSettings settings)
        {
            _settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
        }

        public static int ZoneCount => 6;

        /// <summary>Coat left on <paramref name="zone"/>, 0 (clean) to 1 (the full long-sleep coat).</summary>
        public float Level(DustZone zone)
        {
            return _levels[(int)zone];
        }

        /// <summary>
        /// Sets every part's coat for this moment of the wake, and counts what fell since the last call:
        /// <paramref name="progress01"/> is the check's progress during the checks and the performance's during it.
        /// </summary>
        public void Update(OpeningPhase phase, BootCheckStep step, float progress01)
        {
            for (int i = 0; i < ZoneCount; i++)
            {
                float level = Compute((DustZone)i, phase, step, progress01);
                float drop = _levels[i] - level;
                if (drop > 0f)
                {
                    _fallen[i] += drop;
                }

                _levels[i] = level;
            }
        }

        /// <summary>Takes one puff's worth of fallen coat from a part, if enough has fallen.</summary>
        public bool TryTakePuff(out DustZone zone)
        {
            for (int i = 0; i < ZoneCount; i++)
            {
                if (_fallen[i] >= _settings.DustPuffStep)
                {
                    _fallen[i] -= _settings.DustPuffStep;
                    zone = (DustZone)i;
                    return true;
                }
            }

            zone = DustZone.Body;
            return false;
        }

        private float Compute(DustZone zone, OpeningPhase phase, BootCheckStep step, float p)
        {
            switch (phase)
            {
                case OpeningPhase.FadeOut:
                case OpeningPhase.PushIn:
                case OpeningPhase.Dark:
                case OpeningPhase.PowerOn:
                case OpeningPhase.Logo:
                case OpeningPhase.Load:
                    return 1f;
                case OpeningPhase.Post:
                    return DuringChecks(zone, step, p);
                case OpeningPhase.Ready:
                case OpeningPhase.PullBack:
                    return RestOf(zone);
                case OpeningPhase.Perform:
                    return RestOf(zone) * (1f - PostMotions.DustOffProgress(p));
                default:
                    return 0f;
            }
        }

        /// <summary>Coat left on <paramref name="zone"/> after the checks: all of it when its check does not run.</summary>
        private float RestOf(DustZone zone)
        {
            PostDustSegment segment = _settings.DustSegmentOf(zone);
            return _settings.Runs(segment.Step) ? segment.Rest : 1f;
        }

        private float DuringChecks(DustZone zone, BootCheckStep step, float p)
        {
            PostDustSegment segment = _settings.DustSegmentOf(zone);
            if (!_settings.Runs(segment.Step) || step < segment.Step)
            {
                return 1f;
            }

            if (step > segment.Step)
            {
                return segment.Rest;
            }

            float span = Mathf.Max(segment.End - segment.Start, 1e-4f);
            float eased = WakeOpening.SmoothStepC2(0f, 1f, (p - segment.Start) / span);
            return Mathf.Lerp(1f, segment.Rest, eased);
        }
    }
}
