using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's rear cargo hatch (M3-25 T1): how far and how softly it swings, the "thunk" as it shuts, the shake when a
    /// bundle bumps it shut, and their rumbles.
    /// </summary>
    [Serializable]
    public sealed class CargoHatchSettings
    {
        [Tooltip("How far (deg) the hatch swings open about its top hinge.")]
        [Range(10f, 120f)]
        [SerializeField] private float _openAngle = 55f;

        [Tooltip("Seconds before a bundle arrives that the hatch starts to open.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _openLead = 0.12f;

        [Tooltip("Spring frequency (Hz) of the hatch swinging: eased, never snapped.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _swingFrequency = 3.5f;

        [Tooltip("Damping ratio of the swing (1 settles without overshoot).")]
        [Range(0.3f, 1.5f)]
        [SerializeField] private float _swingDamping = 0.9f;

        [Tooltip("The shut: the share of its speed the hatch bounces back with as it meets the frame (the soft thunk).")]
        [Range(0f, 0.6f)]
        [SerializeField] private float _thunkBounce = 0.2f;

        [Tooltip("How far past the frame (deg) a closing hatch aims, so it meets the frame moving and thunks.")]
        [Range(0f, 40f)]
        [SerializeField] private float _closePush = 15f;

        [Tooltip("Slowest closing speed (deg/s) that still thunks: the bounce's small settle stays silent.")]
        [Range(0f, 200f)]
        [SerializeField] private float _thunkMinSpeed = 30f;

        [Tooltip("A bundle bumps the shut hatch (hold full): its shake's kick (deg).")]
        [Range(0f, 15f)]
        [SerializeField] private float _bumpShake = 4f;

        [Tooltip("Spring frequency (Hz) of the bump's shake.")]
        [Range(1f, 20f)]
        [SerializeField] private float _shakeFrequency = 9f;

        [Tooltip("Damping ratio of the bump's shake (low: a few wobbles).")]
        [Range(0.1f, 1f)]
        [SerializeField] private float _shakeDamping = 0.3f;

        [Tooltip("The thunk as the hatch shuts: 0.05 for 60 ms.")]
        [SerializeField] private RumbleProfile _thunkRumble = new RumbleProfile(0.05f, 0.05f, 0.04f, 0.04f, 0.06f, false);

        [Tooltip("A bundle bumping the shut hatch: 0.05 for 80 ms.")]
        [SerializeField] private RumbleProfile _bumpRumble = new RumbleProfile(0.05f, 0.05f, 0.05f, 0.05f, 0.08f, false);

        public float OpenAngle => _openAngle;

        public float OpenLead => _openLead;

        public float SwingFrequency => _swingFrequency;

        public float SwingDamping => _swingDamping;

        public float ThunkBounce => _thunkBounce;

        public float ThunkMinSpeed => _thunkMinSpeed;

        public float ClosePush => _closePush;

        public float BumpShake => _bumpShake;

        public float ShakeFrequency => _shakeFrequency;

        public float ShakeDamping => _shakeDamping;

        public RumbleProfile ThunkRumble => _thunkRumble;

        public RumbleProfile BumpRumble => _bumpRumble;
    }
}
