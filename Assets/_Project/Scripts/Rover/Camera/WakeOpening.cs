using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The rig's held frame on 07's closed face while a fresh game waits on its title and through New Game's fade to
    /// black (docs/features/M3-09): <see cref="Weight"/> is 1 while it holds, and after a while untouched the frame
    /// starts to drift slowly round 07 (<see cref="DriftYaw"/>). The opening's handover ends it at once in the black
    /// (<see cref="End"/>); nothing eases it out.
    /// </summary>
    public sealed class WakeOpening
    {
        private readonly RoverCameraTuning _tuning;
        private bool _driftFrozen;
        private float _untouched;
        private float _driftClock;
        private float _drift;

        public WakeOpening(RoverCameraTuning tuning)
        {
            _tuning = tuning != null ? tuning : throw new ArgumentNullException(nameof(tuning));
        }

        /// <summary>Whether the frame holds.</summary>
        public bool IsActive { get; private set; }

        /// <summary>The frame's share of the camera: 1 while it holds, 0 once ended.</summary>
        public float Weight => IsActive ? 1f : 0f;

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

        /// <summary>The title's slow drift round the held frame (deg of yaw); 0 once ended.</summary>
        public float DriftYaw { get; private set; }

        /// <summary>Starts holding the frame; ignored while it already holds.</summary>
        public void Begin()
        {
            if (IsActive)
            {
                return;
            }

            _driftFrozen = false;
            _untouched = 0f;
            _driftClock = 0f;
            _drift = 0f;
            DriftYaw = 0f;
            IsActive = true;
        }

        /// <summary>
        /// Keeps the title shot's swing where it is: New Game fades the picture out without moving the camera, so the
        /// swing neither continues nor eases back.
        /// </summary>
        public void FreezeDrift()
        {
            _driftFrozen = true;
        }

        /// <summary>Ends the frame at once, with no ease (the handover's cut in the black).</summary>
        public void End()
        {
            IsActive = false;
            DriftYaw = 0f;
        }

        public void Step(float deltaTime)
        {
            if (!IsActive || deltaTime <= 0f || _driftFrozen)
            {
                return;
            }

            OpeningSettings settings = _tuning.Opening;
            _untouched += deltaTime;
            if (_untouched < settings.TitleDriftDelay)
            {
                return;
            }

            _drift = Mathf.MoveTowards(_drift, 1f, deltaTime / settings.TitleDriftEase);
            _driftClock += deltaTime;
            float swing = Mathf.Sin(2f * Mathf.PI * _driftClock / settings.TitleDriftPeriod);
            DriftYaw = settings.TitleDriftYaw * swing * SmoothStepC2(0f, 1f, _drift);
        }
    }
}
