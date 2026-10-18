using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The first beats of a fresh game (M3-16 P5). Beat 1 holds a low front three-quarter shot on 07's lens as it wakes
    /// and looks up at the stars; beat 2 eases round, as 07 turns its head to Earth, to a low shot over its shoulder
    /// aimed at Earth. Any input, or the hold running out, then eases into the chase frame.
    /// <see cref="Weight"/> is the opening's share of the camera; <see cref="Turn"/> is how far beat 2 has taken over.
    /// Begun held, beat 1 stays put with no input or clock moving it: the fresh title's frame on the closed lens
    /// (docs/features/M3-09), drifting slowly round it after a while untouched (<see cref="DriftYaw"/>), and the frame
    /// the opening's pull-out lands on. <see cref="StartTurn"/> lets beat 2 begin and <see cref="EaseOut"/> hands over
    /// to the chase frame when the opening says so.
    /// </summary>
    public sealed class WakeOpening
    {
        /// <summary>Longest the title's drift may take to ease back once a choice is made (s).</summary>
        private const float MaxDriftReturn = 1f;

        private readonly RoverCameraTuning _tuning;
        private float _held;
        private float _eased;
        private bool _hold;
        private bool _driftFrozen;
        private float _untouched;
        private float _driftClock;
        private float _drift;

        public WakeOpening(RoverCameraTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
        }

        /// <summary>Whether the opening holds or is easing out.</summary>
        public bool IsActive { get; private set; }

        /// <summary>Whether it has started easing out to the chase frame.</summary>
        public bool IsEasing { get; private set; }

        /// <summary>The opening's share of the camera, 1 while holding, 0 once handed to the chase frame.</summary>
        public float Weight { get; private set; }

        /// <summary>Beat 2's share of the opening: 0 on the lens, easing to 1 over Earth; 0 when inactive.</summary>
        public float Turn { get; private set; }

        /// <summary>
        /// Composer screen offset (+ = lower) that puts Earth, <paramref name="earthElevation"/> deg above the horizon,
        /// at viewport height <paramref name="earthHeight"/> for a camera at <paramref name="cameraElevation"/> deg
        /// above the follow point (which then sits that offset below the frame's centre).
        /// </summary>
        public static float EarthScreenY(float earthElevation, float cameraElevation, float earthHeight,
            float verticalFov)
        {
            float halfHeight = Mathf.Tan(0.5f * verticalFov * Mathf.Deg2Rad);
            float pitch = earthElevation - Mathf.Atan(2f * (earthHeight - 0.5f) * halfHeight) * Mathf.Rad2Deg;
            return Mathf.Tan((pitch + cameraElevation) * Mathf.Deg2Rad) / (2f * halfHeight);
        }

        /// <summary>
        /// C2-continuous step from 0 to 1 over [<paramref name="a"/>, <paramref name="b"/>] with zero velocity and
        /// acceleration at both endpoints and a gentle peak slope (&lt; 1.60) that avoids circular-orbit jerk spikes.
        /// </summary>
        public static float SmoothStepC2(float a, float b, float t)
        {
            if (b <= a)
            {
                return t >= b ? 1f : 0f;
            }

            float u = Mathf.Clamp01((t - a) / (b - a));
            float u2 = u * u;
            float u3 = u2 * u;
            float u4 = u2 * u2;
            float om = 1f - u;
            float om2 = om * om;
            float om4 = om2 * om2;
            return u3 * (10f + u * (-15f + 6f * u)) + 27f * u4 * om4 * (1f - 2f * u);
        }

        /// <summary>Whether beat 1 is held (the title, or the opening still inside the lens).</summary>
        public bool IsHeld => IsActive && _hold;

        /// <summary>The title's slow drift round the lens shot (deg of yaw); 0 once released.</summary>
        public float DriftYaw { get; private set; }

        /// <summary>Starts the opening; ignored while one is already running.</summary>
        /// <param name="held">Hold beat 1 until <see cref="StartTurn"/> or <see cref="EaseOut"/>.</param>
        public void Begin(bool held = false)
        {
            if (IsActive)
            {
                return;
            }

            _hold = held;
            _driftFrozen = false;
            _untouched = 0f;
            _driftClock = 0f;
            _drift = 0f;
            DriftYaw = 0f;
            IsActive = true;
            IsEasing = false;
            _held = 0f;
            _eased = 0f;
            Weight = 1f;
            Turn = 0f;
        }

        /// <summary>
        /// Keeps the title shot's swing where it is: New Game fades the picture out without moving the camera, so the
        /// swing neither continues nor eases back.
        /// </summary>
        public void FreezeDrift()
        {
            _driftFrozen = true;
        }

        /// <summary>Lets beat 2 begin now: the camera eases round over 07's shoulder toward Earth.</summary>
        public void StartTurn()
        {
            if (!IsActive || IsEasing)
            {
                return;
            }

            _hold = false;
            _held = Mathf.Max(_held, _tuning.WakeLensSeconds);
        }

        /// <summary>Starts easing out to the chase frame now (the opening's last beat, or the player hurrying it).</summary>
        public void EaseOut()
        {
            if (!IsActive)
            {
                return;
            }

            _hold = false;
            IsEasing = true;
        }

        /// <summary>Ends the opening at once, with no ease (the handover's cut in the black).</summary>
        public void End()
        {
            IsActive = false;
            IsEasing = false;
            _hold = false;
            Weight = 0f;
            Turn = 0f;
            DriftYaw = 0f;
        }

        /// <param name="playerInput">The player gave look or drive input this frame.</param>
        public void Step(bool playerInput, float deltaTime)
        {
            if (!IsActive || deltaTime <= 0f)
            {
                return;
            }

            StepDrift(deltaTime);
            if (_hold)
            {
                return;
            }

            if (!IsEasing)
            {
                _held += deltaTime;
                float turnStart = _tuning.WakeLensSeconds;
                Turn = SmoothStepC2(turnStart, turnStart + _tuning.WakeTurnSeconds, _held);
                IsEasing = playerInput || _held >= _tuning.WakeHoldSeconds;
                return;
            }

            _eased = Math.Min(1f, _eased + deltaTime / _tuning.WakeEaseSeconds);
            Weight = 1f - SmoothStepC2(0f, 1f, _eased);
            if (_eased >= 1f)
            {
                IsActive = false;
                IsEasing = false;
                Weight = 0f;
                Turn = 0f;
                DriftYaw = 0f;
            }
        }

        /// <summary>
        /// Untouched on the title for a while, the held lens shot starts to swing slowly round 07; released, the swing
        /// eases back within a second.
        /// </summary>
        private void StepDrift(float deltaTime)
        {
            if (_driftFrozen)
            {
                return;
            }

            OpeningSettings settings = _tuning.Opening;
            _untouched += deltaTime;
            bool drifting = _hold && _untouched >= settings.TitleDriftDelay;
            float ease = drifting ? settings.TitleDriftEase : Mathf.Min(settings.TitleDriftEase, MaxDriftReturn);
            _drift = Mathf.MoveTowards(_drift, drifting ? 1f : 0f, deltaTime / ease);
            if (drifting)
            {
                _driftClock += deltaTime;
            }

            float swing = Mathf.Sin(2f * Mathf.PI * _driftClock / settings.TitleDriftPeriod);
            DriftYaw = settings.TitleDriftYaw * swing * SmoothStepC2(0f, 1f, _drift);
        }
    }
}
