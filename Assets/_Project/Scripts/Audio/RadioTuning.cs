using UnityEngine;

namespace MoonProject.Audio
{
    /// <summary>
    /// The radio's static: how far the clear signal reaches from the base, how clarity maps to the static's level, how
    /// the set crackles on when 07 wakes, and how the hiss clears when the base powers on. Created by the Audio/Tuning
    /// builder; runtime code only reads it.
    /// </summary>
    public sealed class RadioTuning : ScriptableObject
    {
        [Header("Signal field")]
        [Tooltip("Metres from the base where the signal is perfectly clear.")]
        [Min(0f)] [SerializeField] private float _signalRadius = 60f;

        [Tooltip("Metres over which the signal fades from clear to gone beyond the radius.")]
        [Min(1f)] [SerializeField] private float _falloffWidth = 140f;

        [Tooltip("Seconds (time constant) smoothing the clarity the player hears.")]
        [Min(0f)] [SerializeField] private float _claritySmoothing = 0.8f;

        [Tooltip("Metres the radio sounds farther from home while 07 crawls on its reserve (docs/features/M3-20): the "
            + "static thickens a little, and settles as 07 charges.")]
        [Range(0f, 200f)] [SerializeField] private float _crawlDistance = 60f;

        [Header("Clarity -> static")]
        [Tooltip("Static volume with no signal (scales the static cue): a quiet hiss, never a wall of noise.")]
        [Range(0f, 1f)] [SerializeField] private float _staticMaxVolume = 0.11f;

        [Tooltip("Static volume with a perfect signal (a barely-there presence).")]
        [Range(0f, 1f)] [SerializeField] private float _staticFloorVolume = 0.0075f;

        [Tooltip("Shape of clarity -> static (above 1 drops the static faster as the signal improves).")]
        [Range(0.2f, 4f)] [SerializeField] private float _staticCurve = 1.3f;

        [Tooltip("Volume scale of the tuning swish as the set powers on.")]
        [Range(0f, 1f)] [SerializeField] private float _tuneSwishVolume = 0.8f;

        [Header("Paused: listening in the cabin")]
        [Tooltip("Static volume scale while paused (the cabin shelters the set a little).")]
        [Range(0f, 1f)] [SerializeField] private float _cabinStaticGain = 0.7f;

        [Header("Wake-up (the radio crackles on when 07 wakes)")]
        [Tooltip("Seconds of full wake-up crackle after 07 starts waking on its own before it starts to settle.")]
        [Min(0f)] [SerializeField] private float _wakeSettleDelay = 1.6f;

        [Tooltip("Seconds over which the crackle then settles into the plain hiss.")]
        [Min(0.01f)] [SerializeField] private float _wakeSettleTime = 4f;

        [Tooltip("Crackle delay when the player woke 07 by driving (quicker wake-up).")]
        [Min(0f)] [SerializeField] private float _playerWakeSettleDelay = 0.6f;

        [Tooltip("Crackle settle time when the player woke 07 by driving.")]
        [Min(0.01f)] [SerializeField] private float _playerWakeSettleTime = 1.8f;

        [Tooltip("Seconds for the set to power on (static and swish swell in instead of switching on).")]
        [Min(0.01f)] [SerializeField] private float _wakePowerTime = 0.3f;

        [Tooltip("Extra static while the set is still crackling on (melts away as it settles).")]
        [Range(0f, 1f)] [SerializeField] private float _wakeStaticBoost = 0.55f;

        [Header("Power on (opening v3 R4): the hiss clears")]
        [Tooltip("Static level before the base has power, while the set hears only a faint signal (a fresh game " +
            "hears only this, 07's servos and the room tone).")]
        [Range(0f, 1f)] [SerializeField] private float _unpoweredStaticFloor = 0.3f;

        [Tooltip("Time constant (s) of the hiss clearing once the base powers on.")]
        [Min(0.01f)] [SerializeField] private float _powerOnClearTime = 2.5f;

        public float SignalRadius => _signalRadius;
        public float FalloffWidth => _falloffWidth;

        public float CrawlDistance => _crawlDistance;
        public float ClaritySmoothing => _claritySmoothing;
        public float StaticMaxVolume => _staticMaxVolume;
        public float StaticFloorVolume => _staticFloorVolume;
        public float StaticCurve => _staticCurve;
        public float TuneSwishVolume => _tuneSwishVolume;
        public float CabinStaticGain => _cabinStaticGain;
        public float WakeSettleDelay => _wakeSettleDelay;
        public float WakeSettleTime => _wakeSettleTime;
        public float PlayerWakeSettleDelay => _playerWakeSettleDelay;
        public float PlayerWakeSettleTime => _playerWakeSettleTime;
        public float WakePowerTime => _wakePowerTime;
        public float WakeStaticBoost => _wakeStaticBoost;
        public float UnpoweredStaticFloor => _unpoweredStaticFloor;
        public float PowerOnClearTime => _powerOnClearTime;
    }
}
