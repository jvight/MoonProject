using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// A number that counts towards its target instead of jumping: each change eases out over a duration that grows
    /// with the size of the change (bounded), and a new target mid-count continues from the number on screen.
    /// </summary>
    internal sealed class CountUp
    {
        private readonly MaterialsChipSettings _settings;
        private float _from;
        private float _elapsed;
        private float _duration;

        public CountUp(MaterialsChipSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        public int Target { get; private set; }

        /// <summary>The number to show right now.</summary>
        public int Shown { get; private set; }

        public bool IsSettled => Shown == Target;

        public void Snap(int value)
        {
            Target = value;
            Shown = value;
            _from = value;
            _elapsed = 0f;
            _duration = 0f;
        }

        public void SetTarget(int target)
        {
            if (target == Target)
            {
                return;
            }

            _from = Current();
            Target = target;
            _elapsed = 0f;
            _duration = Mathf.Clamp(Mathf.Abs(target - _from) * _settings.SecondsPerUnit, _settings.MinCountSeconds,
                _settings.MaxCountSeconds);
        }

        /// <summary>
        /// Advances the count; returns true when <see cref="Shown"/> changed (update the label then only).
        /// </summary>
        public bool Step(float deltaTime)
        {
            if (IsSettled)
            {
                return false;
            }

            _elapsed += deltaTime;
            int shown = _elapsed >= _duration ? Target : Mathf.RoundToInt(Current());
            bool changed = shown != Shown;
            Shown = shown;
            return changed;
        }

        private float Current()
        {
            if (_duration <= 0f || _elapsed >= _duration)
            {
                return Target;
            }

            return Mathf.Lerp(_from, Target, UiEase.OutCubic(_elapsed / _duration));
        }
    }
}
