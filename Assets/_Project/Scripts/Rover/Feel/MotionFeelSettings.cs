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

        [Header("Landing")]
        [Tooltip("Landing squash: 07 compresses down on touchdown (m, negative = down), scaled by impact and springing back.")]
        [SerializeField] private NudgeSettings _landSquash = new NudgeSettings(-0.045f, 0f, 3.2f, 0.52f);

        [Tooltip("Minimum share of landing squash on a soft cushioned landing.")]
        [Range(0f, 1f)]
        [SerializeField] private float _landSquashSoft = 0.45f;

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

        [Header("Collision")]
        [Tooltip("Collision jolt: nose dip (deg) and heave compression (m) when 07 bumps an obstacle, scaled by impact.")]
        [SerializeField] private NudgeSettings _collisionJolt = new NudgeSettings(-0.025f, 3.2f, 3.8f, 0.5f);

        [Tooltip("Collision rumble (scaled by impact, for 100 ms).")]
        [SerializeField] private RumbleProfile _collisionRumble = new RumbleProfile(0.28f, 0.28f, 0.22f, 0.22f, 0.1f, false);

        [Tooltip("Impact speed (m/s) counted as a full-strength collision jolt.")]
        [Range(1f, 15f)]
        [SerializeField] private float _collisionHardSpeed = 6f;

        [Header("Low battery and boost")]
        [Tooltip("The battery empties: 0.06 decaying over 600 ms.")]
        [SerializeField] private RumbleProfile _powerDown = new RumbleProfile(0.06f, 0f, 0.03f, 0f, 0.6f, false);

        [Header("Cargo")]
        [Tooltip("How far (m) the suspension sags per quarter of the hold filled (visual only, M3-25).")]
        [Range(0f, 0.05f)]
        [SerializeField] private float _sagPerQuarter = 0.01f;

        [Tooltip("Half-life (s) of the sag settling to a new load.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _sagHalfLife = 0.4f;

        [Tooltip("The radio tower comes online: 0.3 for 120 ms (feel-pass §8.2).")]
        [SerializeField] private RumbleProfile _towerOnline = new RumbleProfile(0.3f, 0.3f, 0.25f, 0.25f, 0.12f, false);

        [Tooltip("The drums' boost: a soft 0.06 while it lasts.")]
        [SerializeField] private RumbleProfile _boost = new RumbleProfile(0.06f, 0.06f, 0.03f, 0.03f, 0f, true);

        public float StartSpeed => _startSpeed;

        public float StopSpeed => _stopSpeed;

        public NudgeSettings StartSquat => _startSquat;

        public NudgeSettings StopDip => _stopDip;

        public RumbleProfile StartRumble => _startRumble;

        public RumbleProfile StopRumble => _stopRumble;

        public NudgeSettings LandSquash => _landSquash;

        /// <summary>The landing squash's scale for an impact of share <paramref name="impact"/> (0..1).</summary>
        public float LandSquashScale(float impact)
        {
            return impact <= 0f ? 0f : Mathf.Lerp(_landSquashSoft, 1f, Mathf.Clamp01(impact));
        }

        public RumbleProfile LandRumble => _landRumble;

        /// <summary>The landing rumble's level for an impact of share <paramref name="impact"/> (0..1).</summary>
        public float LandLevel(float impact)
        {
            return Mathf.Lerp(_landRumbleSoft, _landRumbleHard, Mathf.Clamp01(impact));
        }

        public float HardLanding => _hardLanding;

        public NudgeSettings CollisionJolt => _collisionJolt;

        public RumbleProfile CollisionRumble => _collisionRumble;

        public float CollisionHardSpeed => _collisionHardSpeed;

        public RumbleProfile PowerDown => _powerDown;

        public RumbleProfile Boost => _boost;

        public RumbleProfile TowerOnline => _towerOnline;

        public float SagPerQuarter => _sagPerQuarter;

        public float SagHalfLife => _sagHalfLife;
    }
}
