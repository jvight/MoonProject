using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// How one kit piece or gift comes onto 07, stepped once per frame. Hidden until shown: a loaded game shows it at
    /// once (<see cref="Show"/>); a purchase or a gift installs it (<see cref="Install"/>): after a short wait for the
    /// camera it appears small just above its socket (a gift: from nothing, in place), grows, drops and settles with
    /// one small overshoot, reporting the frame it lands. Its lights then ease on.
    /// </summary>
    public sealed class KitFit
    {
        /// <summary>Close enough to rest that the springs stop.</summary>
        private const float Rest = 1e-3f;

        private readonly KitSettings _settings;
        private readonly bool _gift;
        private DampedSpring _drop;
        private DampedSpring _scale;
        private float _wait;
        private bool _landed;
        private bool _settledThisStep;

        private enum Phase
        {
            Hidden,
            Waiting,
            Settling,
            Fitted,
        }

        private Phase _phase;

        /// <param name="gift">A friend's gift (grows in place) rather than a kit piece (drops onto its socket).</param>
        public KitFit(KitSettings settings, bool gift)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _gift = gift;
            _scale.Reset(1f);
        }

        /// <summary>The piece is on 07 (settling or fitted).</summary>
        public bool Visible => _phase == Phase.Settling || _phase == Phase.Fitted;

        /// <summary>Hidden and not on its way: a purchase or a gift may start it.</summary>
        public bool IsHidden => _phase == Phase.Hidden;

        /// <summary>Its pose changed this step (settling in, or the step it came to rest): write it out.</summary>
        public bool Moving => _phase == Phase.Settling || _settledThisStep;

        /// <summary>Height (m) above its socket right now (0 at rest; a little below 0 at the overshoot).</summary>
        public float Offset => _drop.Value;

        /// <summary>Uniform scale right now (1 at rest).</summary>
        public float Scale => Mathf.Max(0f, _scale.Value);

        /// <summary>How far its lights are on, 0..1 (eased on once it has landed).</summary>
        public float Lights { get; private set; }

        /// <summary>There at once, lights on, no moment (a loaded game).</summary>
        public void Show()
        {
            _phase = Phase.Fitted;
            _drop.Reset(0f);
            _scale.Reset(1f);
            _landed = true;
            Lights = 1f;
        }

        /// <summary>Starts installing: a short wait, then it appears and settles in.</summary>
        public void Install()
        {
            _phase = Phase.Waiting;
            _wait = _gift ? _settings.GiftDelay : _settings.FitDelay;
            _landed = false;
            Lights = 0f;
        }

        /// <summary>Advances one frame; true on the frame the piece lands on 07.</summary>
        public bool Step(float deltaTime)
        {
            _settledThisStep = false;
            if (deltaTime <= 0f)
            {
                return false;
            }

            switch (_phase)
            {
                case Phase.Waiting:
                    _wait -= deltaTime;
                    if (_wait <= 0f)
                    {
                        Appear();
                    }

                    return false;

                case Phase.Settling:
                    return Settle(deltaTime);

                case Phase.Fitted:
                    Lights = Smoothing.Damp(Lights, 1f, _settings.LightsOnHalfLife, deltaTime);
                    return false;

                default:
                    return false;
            }
        }

        private void Appear()
        {
            _phase = Phase.Settling;
            _drop.Reset(_gift ? 0f : _settings.FitDrop);
            _scale.Reset(_gift ? 0f : _settings.FitStartScale);
        }

        private bool Settle(float deltaTime)
        {
            bool landed = false;
            if (_gift)
            {
                _scale.Step(1f, _settings.GiftFrequency, _settings.GiftDamping, deltaTime);
                landed = !_landed && _scale.Value >= 1f;
            }
            else
            {
                _drop.Step(0f, _settings.FitFrequency, _settings.FitDamping, deltaTime);
                _scale.Step(1f, _settings.FitGrowFrequency, 1f, deltaTime);
                landed = !_landed && _drop.Value <= 0f;
            }

            _landed |= landed;
            if (_landed)
            {
                Lights = Smoothing.Damp(Lights, 1f, _settings.LightsOnHalfLife, deltaTime);
            }

            bool still = Mathf.Abs(_drop.Value) < Rest && Mathf.Abs(_drop.Velocity) < Rest
                && Mathf.Abs(_scale.Value - 1f) < Rest && Mathf.Abs(_scale.Velocity) < Rest;
            if (_landed && still)
            {
                _phase = Phase.Fitted;
                _drop.Reset(0f);
                _scale.Reset(1f);
                _settledThisStep = true;
            }

            return landed;
        }
    }
}
