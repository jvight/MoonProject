using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// A number that rolls towards its target one unit at a time (docs/design/feel-pass.md R7: counters count, never
    /// jump): a unit every <see cref="MaterialsChipSettings.StepSeconds"/>, the first at once, none ever skipped. A
    /// big change quickens the step so the whole roll stays within <see cref="MaterialsChipSettings.MaxCountSeconds"/>,
    /// and a new target mid-roll carries on from the number on screen.
    /// </summary>
    internal sealed class CountUp
    {
        private readonly MaterialsChipSettings _settings;
        private float _stepSeconds;
        private float _clock;

        public CountUp(MaterialsChipSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public int Target { get; private set; }

        /// <summary>The number to show right now.</summary>
        public int Shown { get; private set; }

        public bool IsSettled => Shown == Target;

        /// <summary>The units the last <see cref="Step"/> moved, signed (0 when it moved none).</summary>
        public int Moved { get; private set; }

        /// <summary>
        /// The units moved in this roll so far: it keeps counting while a second change extends the roll, and starts
        /// again once the count has rested or turned the other way.
        /// </summary>
        public int Rolled { get; private set; }

        /// <summary>The units still to come.</summary>
        public int Remaining => Mathf.Abs(Target - Shown);

        public void Snap(int value)
        {
            Target = value;
            Shown = value;
            Moved = 0;
            Rolled = 0;
            _clock = 0f;
            _stepSeconds = _settings.StepSeconds;
        }

        public void SetTarget(int target)
        {
            if (target == Target)
            {
                return;
            }

            bool resting = IsSettled;
            if (resting || Math.Sign(target - Shown) != Math.Sign(Target - Shown))
            {
                Rolled = 0;
                _clock = 0f;
            }

            Target = target;
            _stepSeconds = Mathf.Min(_settings.StepSeconds, _settings.MaxCountSeconds / Mathf.Max(1, Remaining));
            if (resting)
            {
                _clock = _stepSeconds;
            }
        }

        /// <summary>
        /// Advances the roll; returns true when <see cref="Shown"/> changed (update the label then only).
        /// </summary>
        public bool Step(float deltaTime)
        {
            Moved = 0;
            if (IsSettled)
            {
                return false;
            }

            _clock += deltaTime;
            int direction = Math.Sign(Target - Shown);
            while (_clock >= _stepSeconds && Shown != Target)
            {
                Shown += direction;
                Moved += direction;
                Rolled++;
                _clock -= _stepSeconds;
            }

            if (Shown == Target)
            {
                _clock = 0f;
            }

            return Moved != 0;
        }
    }
}
