using UnityEngine;
using MoonProject.Core;

namespace MoonProject.UI
{
    /// <summary>
    /// The hold ring's charge segment (docs/design/feel-pass.md §3.1, pure logic, EditMode-tested): from a cut or dig's
    /// Anticipate beat it fills over the beat's seconds (0.65 s fresh, 0.3 s on a resume), holds full until ignition,
    /// then hands over to the progress fill by easing away; a Cancel empties it. The ring's own progress is kept by
    /// the game, so a release never loses it (ruling 4).
    /// </summary>
    internal sealed class ChargeSegment
    {
        private enum Phase
        {
            Idle,
            Filling,
            Full,
            Draining,
        }

        private Phase _phase;
        private float _fillSeconds;
        private float _age;
        private float _drainFrom;
        private float _drainSeconds;

        /// <summary>The segment's fill, 0..1.</summary>
        public float Value { get; private set; }

        /// <summary>True from the charge's start until the segment has emptied.</summary>
        public bool IsActive => _phase != Phase.Idle;

        /// <summary>True while the segment is filling or full, before ignition or a cancel.</summary>
        public bool IsCharging => _phase == Phase.Filling || _phase == Phase.Full;

        /// <summary>Whether this charge belongs to a dig (it has no cut progress to show) or a cut.</summary>
        public FeelKind Kind { get; private set; }

        /// <summary>Where the charge is in the world (the cut point or the dig's mouth).</summary>
        public Vector3 Point { get; private set; }

        /// <summary>The charge begins: it fills over <paramref name="seconds"/>.</summary>
        public void Begin(FeelKind kind, Vector3 point, float seconds)
        {
            Kind = kind;
            Point = point;
            _fillSeconds = Mathf.Max(seconds, 0.01f);
            _age = 0f;
            Value = 0f;
            _phase = Phase.Filling;
        }

        /// <summary>Ignition: the segment is full and eases away over <paramref name="handoffSeconds"/>.</summary>
        public void Ignite(float handoffSeconds)
        {
            if (IsCharging)
            {
                Value = 1f;
                Drain(handoffSeconds);
            }
        }

        /// <summary>The player let go before ignition: the segment empties over <paramref name="emptySeconds"/>.</summary>
        public void Cancel(float emptySeconds)
        {
            if (IsCharging)
            {
                Drain(emptySeconds);
            }
        }

        public void Step(float deltaTime)
        {
            switch (_phase)
            {
                case Phase.Filling:
                    _age += deltaTime;
                    Value = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(_age / _fillSeconds));
                    if (_age >= _fillSeconds)
                    {
                        _phase = Phase.Full;
                    }

                    break;
                case Phase.Draining:
                    _age += deltaTime;
                    Value = _drainFrom * (1f - Mathf.Clamp01(_age / _drainSeconds));
                    if (_age >= _drainSeconds)
                    {
                        Value = 0f;
                        _phase = Phase.Idle;
                    }

                    break;
            }
        }

        private void Drain(float seconds)
        {
            _drainFrom = Value;
            _drainSeconds = Mathf.Max(seconds, 0.01f);
            _age = 0f;
            _phase = Phase.Draining;
        }
    }
}
