using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// How one kit piece or gift comes onto 07, stepped once per frame. Hidden until shown: a loaded game shows it at
    /// once (<see cref="Show"/>). A crafted piece is carried in by the Rover Bay (<see cref="Carry"/>: its pose is the
    /// bay arm's, it grows from the size it showed in the rack) and fitted where the arm sets it
    /// (<see cref="Land"/>). A friend's gift grows in place from nothing with one soft overshoot after a short wait
    /// (<see cref="Install"/>), reporting the frame it is fully there. Lights ease on once a piece is on.
    /// </summary>
    public sealed class KitFit
    {
        /// <summary>Close enough to rest that the spring stops.</summary>
        private const float Rest = 1e-3f;

        private readonly KitSettings _settings;
        private readonly bool _gift;
        private DampedSpring _scale;
        private float _wait;
        private bool _landed;
        private bool _settledThisStep;
        private bool _landedSinceStep;

        private enum Phase
        {
            Hidden,
            Waiting,
            Carried,
            Growing,
            Fitted,
        }

        private Phase _phase;

        /// <param name="gift">A friend's gift (grows in place) rather than crafted kit (carried in by the bay).</param>
        public KitFit(KitSettings settings, bool gift)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _gift = gift;
            _scale.Reset(1f);
        }

        /// <summary>The piece is on 07 or on its way onto it (carried, growing or fitted).</summary>
        public bool Visible => _phase == Phase.Carried || _phase == Phase.Growing || _phase == Phase.Fitted;

        /// <summary>Hidden and not on its way: a purchase or a gift may start it.</summary>
        public bool IsHidden => _phase == Phase.Hidden;

        /// <summary>In the bay arm's hold: its position is the arm's, not its socket's.</summary>
        public bool IsCarried => _phase == Phase.Carried;

        /// <summary>Its scale changed this step, or it just came to rest: write it out.</summary>
        public bool Moving => _phase == Phase.Carried || _phase == Phase.Growing || _settledThisStep;

        /// <summary>Uniform scale right now (1 at rest).</summary>
        public float Scale => Mathf.Max(0f, _scale.Value);

        /// <summary>How far its lights are on, 0..1 (eased on once it is on 07).</summary>
        public float Lights { get; private set; }

        /// <summary>There at once, lights on, no moment (a loaded game).</summary>
        public void Show()
        {
            _phase = Phase.Fitted;
            _scale.Reset(1f);
            _landed = true;
            Lights = 1f;
        }

        /// <summary>A friend's gift: a short wait, then it grows in place.</summary>
        public void Install()
        {
            if (!_gift)
            {
                throw new InvalidOperationException("Crafted kit is carried in by the Rover Bay, not grown in place.");
            }

            _phase = Phase.Waiting;
            _wait = _settings.GiftDelay;
            _landed = false;
            Lights = 0f;
        }

        /// <summary>
        /// Crafted kit taken by a bay arm: shown, growing from <paramref name="startScale"/> as it comes.
        /// </summary>
        public void Carry(float startScale)
        {
            _phase = Phase.Carried;
            _scale.Reset(startScale);
            _landed = false;
            Lights = 0f;
        }

        /// <summary>The arm set it on its socket: fitted, full size; its lights come on.</summary>
        public void Land()
        {
            _phase = Phase.Fitted;
            _scale.Reset(1f);
            _landed = true;
            _landedSinceStep = true;
        }

        /// <summary>Advances one frame; true on the frame a gift is fully there.</summary>
        public bool Step(float deltaTime)
        {
            _settledThisStep = _landedSinceStep;
            _landedSinceStep = false;
            bool landed = false;
            if (deltaTime > 0f)
            {
                switch (_phase)
                {
                    case Phase.Waiting:
                        _wait -= deltaTime;
                        if (_wait <= 0f)
                        {
                            _phase = Phase.Growing;
                            _scale.Reset(0f);
                        }

                        break;

                    case Phase.Carried:
                        _scale.Step(1f, _settings.FitGrowFrequency, 1f, deltaTime);
                        break;

                    case Phase.Growing:
                        landed = Grow(deltaTime);
                        break;

                    case Phase.Fitted:
                        if (_landed)
                        {
                            Lights = Smoothing.Damp(Lights, 1f, _settings.LightsOnHalfLife, deltaTime);
                        }

                        break;
                }
            }

            return landed;
        }

        private bool Grow(float deltaTime)
        {
            _scale.Step(1f, _settings.GiftFrequency, _settings.GiftDamping, deltaTime);
            bool landed = !_landed && _scale.Value >= 1f;
            _landed |= landed;
            if (_landed)
            {
                Lights = Smoothing.Damp(Lights, 1f, _settings.LightsOnHalfLife, deltaTime);
            }

            if (_landed && Mathf.Abs(_scale.Value - 1f) < Rest && Mathf.Abs(_scale.Velocity) < Rest)
            {
                _phase = Phase.Fitted;
                _scale.Reset(1f);
                _settledThisStep = true;
            }

            return landed;
        }
    }
}
