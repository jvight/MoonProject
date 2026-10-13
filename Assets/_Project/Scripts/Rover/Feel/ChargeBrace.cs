using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07 bracing through the cut's charge-up as plain logic (docs/design/0.5.5-owner-feedback.md §5.3):
    /// <list type="bullet">
    /// <item><see cref="Begin"/>: a fresh charge plants 07, squatting on its suspension over the brace; a warm one
    /// (no brace) drops straight to the braced depth. The wheels lock and the rockers stiffen.</item>
    /// <item><see cref="Stage"/>: each stage lock settles it a little further with a short body-only tremor; the full
    /// lock settles without one and stays dead still.</item>
    /// <item><see cref="Release"/>: 07 rises back with one soft overshoot and the rockers follow the ground again.</item>
    /// </list>
    /// Heights in metres, down negative. Zero allocations.
    /// </summary>
    public sealed class ChargeBrace
    {
        /// <summary>Released rockers this loose are free again (they take the ground's pitch afresh next brace).</summary>
        private const float RestStiffness = 1e-3f;

        private readonly ChargeBraceSettings _settings;
        private DampedSpring _height;
        private float _target;
        private float _frequency;
        private float _damping = 1f;
        private float _stiffness;
        private float _tremorAge = float.PositiveInfinity;

        public ChargeBrace(ChargeBraceSettings settings)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }

        /// <summary>True from <see cref="Begin"/> until <see cref="Release"/>: the wheels are locked.</summary>
        public bool IsBraced { get; private set; }

        /// <summary>True once the needed stage locked: 07 holds dead still until released.</summary>
        public bool IsLocked { get; private set; }

        /// <summary>The chassis' offset now (m, down negative), the tremor included.</summary>
        public float Heave => _height.Value + Tremor;

        /// <summary>How stiff the rockers are, 0 (following the ground, exactly 0 once free) .. 1 (held where they
        /// were).</summary>
        public float Stiffness => _stiffness;

        /// <summary>Whether everything is back at rest.</summary>
        public bool IsAtRest(float tolerance)
        {
            return !IsBraced && _stiffness < tolerance && Mathf.Abs(_height.Value) < tolerance
                   && Mathf.Abs(_height.Velocity) < tolerance;
        }

        /// <param name="warm">A warm emitter: no brace, straight to the braced depth.</param>
        public void Begin(bool warm)
        {
            IsBraced = true;
            IsLocked = false;
            _tremorAge = float.PositiveInfinity;
            Aim(-_settings.Squat, warm ? _settings.SettleFrequency : _settings.SquatFrequency, 1f);
        }

        /// <summary>Stage <paramref name="stage"/> of <paramref name="needed"/> locked (the last is the full lock).</summary>
        public void Stage(int stage, int needed)
        {
            if (!IsBraced || IsLocked || stage < 1)
            {
                return;
            }

            IsLocked = stage >= needed;
            _tremorAge = IsLocked ? float.PositiveInfinity : 0f;
            Aim(-_settings.Squat - Mathf.Min(stage, needed) * _settings.SettlePerStage, _settings.SettleFrequency, 1f);
        }

        /// <summary>The charge drained or the cut ended: 07 rises back with one soft overshoot.</summary>
        public void Release()
        {
            if (!IsBraced)
            {
                return;
            }

            IsBraced = false;
            IsLocked = false;
            _tremorAge = float.PositiveInfinity;
            Aim(0f, _settings.RiseFrequency, _settings.RiseDamping);
        }

        /// <summary>Back to rest at once (07 was placed).</summary>
        public void Reset()
        {
            IsBraced = false;
            IsLocked = false;
            _tremorAge = float.PositiveInfinity;
            _target = 0f;
            _stiffness = 0f;
            _height.Reset(0f);
        }

        public void Step(float deltaTime)
        {
            float step = Mathf.Max(deltaTime, 0f);
            _height.Step(_target, _frequency, _damping, step);
            _stiffness = Smoothing.Damp(_stiffness, IsBraced ? 1f : 0f, _settings.StiffenHalfLife, step);
            if (!IsBraced && _stiffness < RestStiffness)
            {
                _stiffness = 0f;
            }

            _tremorAge += step;
        }

        private float Tremor
        {
            get
            {
                if (_tremorAge >= _settings.TremorSeconds)
                {
                    return 0f;
                }

                float fade = 1f - _tremorAge / _settings.TremorSeconds;
                return _settings.Tremor * fade * Mathf.Sin(2f * Mathf.PI * _settings.TremorFrequency * _tremorAge);
            }
        }

        private void Aim(float target, float frequency, float damping)
        {
            _target = target;
            _frequency = frequency;
            _damping = damping;
        }
    }
}
