using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's lamps coming on as it opens its eyes (owner playtest): they stay dark while it sleeps, then flicker a few
    /// times unevenly (each flicker its own width and peak, none reaching full), fall dark for a short gap, and ramp
    /// up smoothly to full. Pure and allocation-free; <see cref="Level"/> scales the headlamp and the eye lamp.
    /// </summary>
    public sealed class LampPowerUp
    {
        private const float GoldenRatio = 0.618034f;
        private const float WidthBase = 0.6f;
        private const float WidthSpread = 0.8f;
        private const float PeakBase = 0.35f;
        private const float PeakSpread = 0.45f;
        private const float PeakStep = 0.754f;
        private const float Offset = 0.3f;

        private readonly OpeningSettings _settings;
        private bool _dark;
        private float _clock = -1f;

        public LampPowerUp(OpeningSettings settings)
        {
            _settings = settings ?? throw new System.ArgumentNullException(nameof(settings));
            Level = 1f;
        }

        /// <summary>Lamp power now, 0 (dark) .. 1 (full).</summary>
        public float Level { get; private set; }

        /// <summary>True while the sequence holds the lamps: asleep or powering up. False once they are simply on.</summary>
        public bool IsControlling => _dark || _clock >= 0f;

        /// <summary>The seconds the whole sequence takes from <see cref="Start"/>.</summary>
        public float TotalSeconds => _settings.LampFlickerSeconds + _settings.LampDarkGapSeconds
            + _settings.LampRampSeconds;

        /// <summary>07 sleeps: the lamps are dark until <see cref="Start"/>.</summary>
        public void HoldDark()
        {
            _dark = true;
            _clock = -1f;
            Level = 0f;
        }

        /// <summary>07 opened its eyes: the flicker begins.</summary>
        public void Start()
        {
            _dark = false;
            _clock = 0f;
            Level = Evaluate(0f);
        }

        /// <summary>The lamps are on at once, the sequence is dropped (a scene without a wake).</summary>
        public void Release()
        {
            _dark = false;
            _clock = -1f;
            Level = 1f;
        }

        public void Step(float deltaTime)
        {
            if (_clock < 0f)
            {
                return;
            }

            _clock += Mathf.Max(deltaTime, 0f);
            if (_clock >= TotalSeconds)
            {
                Release();
                return;
            }

            Level = Evaluate(_clock);
        }

        /// <summary>Lamp power at <paramref name="time"/> seconds into the sequence (pure).</summary>
        public float Evaluate(float time)
        {
            float span = _settings.LampFlickerSeconds;
            if (time < span)
            {
                return Flicker(time, span);
            }

            time -= span;
            float gap = _settings.LampDarkGapSeconds;
            if (time < gap)
            {
                return 0f;
            }

            float ramp = Mathf.Max(_settings.LampRampSeconds, 1e-3f);
            return Smoothing.SmoothStep(0f, 1f, (time - gap) / ramp);
        }

        /// <summary>The width of flicker <paramref name="index"/> as a share of the flicker span.</summary>
        public float FlickerShare(int index)
        {
            float total = 0f;
            for (int i = 0; i < _settings.LampFlickerCount; i++)
            {
                total += RawWidth(i);
            }

            return RawWidth(index) / total;
        }

        /// <summary>The peak of flicker <paramref name="index"/> (below full).</summary>
        public static float Peak(int index)
        {
            return PeakBase + PeakSpread * Fraction(index * PeakStep + Offset);
        }

        private float Flicker(float time, float span)
        {
            float start = 0f;
            for (int i = 0; i < _settings.LampFlickerCount; i++)
            {
                float width = FlickerShare(i) * span;
                if (time < start + width)
                {
                    return Peak(i) * Mathf.Sin(Mathf.PI * (time - start) / width);
                }

                start += width;
            }

            return 0f;
        }

        private static float RawWidth(int index)
        {
            return WidthBase + WidthSpread * Fraction(index * GoldenRatio + Offset);
        }

        private static float Fraction(float value)
        {
            return value - Mathf.Floor(value);
        }
    }
}
