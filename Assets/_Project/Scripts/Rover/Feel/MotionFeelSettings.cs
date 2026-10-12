using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// How 07's own verbs feel on its chassis and in the gamepad (docs/design/feel-pass.md §2.3 drive, §2.4 leap,
    /// §2.10 install, §2.13 low battery). The drums' boost also rumbles softly while it lasts.
    /// </summary>
    [Serializable]
    public sealed class MotionFeelSettings
    {
        [Header("Drive")]
        [Tooltip("Speed (m/s) above which 07 has started from rest.")]
        [Range(0.1f, 3f)]
        [SerializeField] private float _startSpeed = 0.6f;

        [Tooltip("Speed (m/s) under which a moving 07 has stopped.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _stopSpeed = 0.25f;

        [Tooltip("Start: 07 squats back (deg nose-up), springing out of it.")]
        [SerializeField] private NudgeSettings _startSquat = new NudgeSettings(0f, 1.5f, 2.5f, 0.8f);

        [Tooltip("Stop: 07 dips its nose with one soft overshoot back.")]
        [SerializeField] private NudgeSettings _stopDip = new NudgeSettings(0f, 2f, 2.5f, 0.6f);

        [Tooltip("Start: a 0.05 swell.")]
        [SerializeField] private RumbleProfile _startRumble = new RumbleProfile(0.05f, 0f, 0.03f, 0f, 0.25f, false);

        [Tooltip("Stop: 0.06 for 120 ms.")]
        [SerializeField] private RumbleProfile _stopRumble = new RumbleProfile(0.06f, 0.06f, 0.04f, 0.04f, 0.12f, false);

        [Header("Leap")]
        [Tooltip("How far (m) 07 crouches at a full charge (2 cm per step of four).")]
        [Range(0f, 0.15f)]
        [SerializeField] private float _crouch = 0.08f;

        [Tooltip("The crouch's spring.")]
        [SerializeField] private NudgeSettings _crouchSpring = new NudgeSettings(0f, 0f, 3f, 1f);

        [Tooltip("Charge rumble at the first step.")]
        [Range(0f, 0.35f)]
        [SerializeField] private float _chargeRumbleBase = 0.04f;

        [Tooltip("Extra charge rumble at a full charge (0.03 per step of four).")]
        [Range(0f, 0.35f)]
        [SerializeField] private float _chargeRumbleFull = 0.12f;

        [Tooltip("The charge rumble's shape (held until the launch), scaled by the levels above.")]
        [SerializeField] private RumbleProfile _chargeRumble = new RumbleProfile(1f, 1f, 0.5f, 0.5f, 0f, true);

        [Tooltip("Launch: 07 stretches up (m), springing back.")]
        [SerializeField] private NudgeSettings _launchStretch = new NudgeSettings(0.02f, 0f, 4f, 0.7f);

        [Tooltip("Launch: 0.25 for 90 ms.")]
        [SerializeField] private RumbleProfile _launchRumble = new RumbleProfile(0.25f, 0.25f, 0.2f, 0.2f, 0.09f, false);

        [Tooltip("Landing rumble (0.1 to 0.3 by impact, for 120 ms), scaled by the impact below.")]
        [SerializeField] private RumbleProfile _landRumble = new RumbleProfile(1f, 1f, 0.7f, 0.7f, 0.12f, false);

        [Tooltip("Landing rumble at the softest landing.")]
        [Range(0f, 0.35f)]
        [SerializeField] private float _landRumbleSoft = 0.1f;

        [Tooltip("Landing rumble at the hardest.")]
        [Range(0f, 0.35f)]
        [SerializeField] private float _landRumbleHard = 0.3f;

        [Tooltip("Impact speed (m/s) counted as the hardest landing.")]
        [Range(0.5f, 20f)]
        [SerializeField] private float _hardLanding = 6f;

        [Header("Install")]
        [Tooltip("The piece seats: 07 dips 1.5 cm.")]
        [SerializeField] private NudgeSettings _seatDip = new NudgeSettings(0.015f, 0f, 3f, 0.6f);

        [Tooltip("The piece seats: 0.2 for 100 ms.")]
        [SerializeField] private RumbleProfile _seatRumble = new RumbleProfile(0.2f, 0.2f, 0.15f, 0.15f, 0.1f, false);

        [Header("Low battery and boost")]
        [Tooltip("The battery empties: 0.06 decaying over 600 ms.")]
        [SerializeField] private RumbleProfile _powerDown = new RumbleProfile(0.06f, 0f, 0.03f, 0f, 0.6f, false);

        [Tooltip("The drums' boost: a soft 0.06 while it lasts.")]
        [SerializeField] private RumbleProfile _boost = new RumbleProfile(0.06f, 0.06f, 0.03f, 0.03f, 0f, true);

        public float StartSpeed => _startSpeed;

        public float StopSpeed => _stopSpeed;

        public NudgeSettings StartSquat => _startSquat;

        public NudgeSettings StopDip => _stopDip;

        public RumbleProfile StartRumble => _startRumble;

        public RumbleProfile StopRumble => _stopRumble;

        public float Crouch => _crouch;

        public NudgeSettings CrouchSpring => _crouchSpring;

        public RumbleProfile ChargeRumble => _chargeRumble;

        /// <summary>The held charge rumble's level at charge <paramref name="strength"/> (0..1).</summary>
        public float ChargeLevel(float strength)
        {
            return _chargeRumbleBase + _chargeRumbleFull * Mathf.Clamp01(strength);
        }

        public NudgeSettings LaunchStretch => _launchStretch;

        public RumbleProfile LaunchRumble => _launchRumble;

        public RumbleProfile LandRumble => _landRumble;

        /// <summary>The landing rumble's level for an impact of share <paramref name="impact"/> (0..1).</summary>
        public float LandLevel(float impact)
        {
            return Mathf.Lerp(_landRumbleSoft, _landRumbleHard, Mathf.Clamp01(impact));
        }

        public float HardLanding => _hardLanding;

        public NudgeSettings SeatDip => _seatDip;

        public RumbleProfile SeatRumble => _seatRumble;

        public RumbleProfile PowerDown => _powerDown;

        public RumbleProfile Boost => _boost;
    }
}
