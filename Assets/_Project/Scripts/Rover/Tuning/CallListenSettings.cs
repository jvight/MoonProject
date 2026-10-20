using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07 listening to the call from the sky (docs/design/opening-v3-purpose.md §2, plan slice 2.2): while it idles or
    /// charges its head turns toward the call's point of light, a broken call stutters the antenna's lamp and now and
    /// then 07 reaches up toward it, and once the call locks the lamp keys the pattern back: three short, one long.
    /// Soft edges everywhere, and nothing blinks faster than three times a second.
    /// </summary>
    [Serializable]
    public sealed class CallListenSettings
    {
        [Header("Head")]
        [Tooltip("Half-life (s) of the head's turn toward the call, and back, as 07 starts or stops idling.")]
        [Range(0.1f, 3f)]
        [SerializeField] private float _headHalfLife = 0.8f;

        [Tooltip("Share of the daydream (0..1) above which 07 counts as idle and turns toward the call.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _idleShare = 0.3f;

        [Header("The broken call: the lamp stutters")]
        [Tooltip("Glow the lamp keeps between stutters while the call is broken (linear fraction of authored).")]
        [Range(0f, 1f)]
        [SerializeField] private float _stutterBase = 0.05f;

        [Tooltip("Extra glow of a stutter's lit bursts.")]
        [Range(0f, 3f)]
        [SerializeField] private float _stutterGlow = 1.1f;

        [Tooltip("Shortest and longest a lit burst or a dark gap lasts (s). The shortest is at least 1/3 s.")]
        [Range(0.34f, 2f)]
        [SerializeField] private float _stutterMinSeconds = 0.4f;

        [Range(0.34f, 3f)]
        [SerializeField] private float _stutterMaxSeconds = 1.3f;

        [Tooltip("Share of the stutter's slots that are lit bursts (the rest are dark gaps).")]
        [Range(0.1f, 0.9f)]
        [SerializeField] private float _stutterLitShare = 0.4f;

        [Header("The reach")]
        [Tooltip("Seconds between 07's reaches toward a broken call while it idles.")]
        [Range(4f, 60f)]
        [SerializeField] private float _reachInterval = 14f;

        [Tooltip("Seconds of idling before the first reach.")]
        [Range(0.5f, 20f)]
        [SerializeField] private float _reachFirstSeconds = 4f;

        [Tooltip("Seconds the reach takes to rise, to hold and to settle back (each eased).")]
        [Range(0.2f, 3f)]
        [SerializeField] private float _reachRise = 0.7f;

        [Range(0f, 4f)]
        [SerializeField] private float _reachHold = 1f;

        [Range(0.2f, 3f)]
        [SerializeField] private float _reachFall = 1.1f;

        [Tooltip("Degrees the head lifts at the top of a reach.")]
        [Range(0f, 40f)]
        [SerializeField] private float _reachHeadPitch = 12f;

        [Tooltip("Degrees the neck lifts toward the sky at the top of a reach (alert lift).")]
        [Range(0f, 20f)]
        [SerializeField] private float _reachNeckLift = 6f;

        [Tooltip("Metres the antenna rises along its own axis at the top of a reach.")]
        [Range(0f, 0.2f)]
        [SerializeField] private float _reachAntennaLift = 0.06f;

        [Header("The clear call and the failed reply")]
        [Tooltip("Steady glow of the antenna lamp while the call is clear (linear fraction of authored).")]
        [Range(0f, 2f)]
        [SerializeField] private float _clearGlow = 0.5f;

        [Tooltip("Seconds 07's head stays lowered after its reply fails (the face shows its line this long).")]
        [Range(0.5f, 8f)]
        [SerializeField] private float _dejectedHoldSeconds = 3.2f;

        [Tooltip("Half-life (s) of the head lowering and of lifting again.")]
        [Range(0.1f, 3f)]
        [SerializeField] private float _dejectedHalfLife = 0.6f;

        [Tooltip("Degrees the head lowers when the reply fails.")]
        [Range(0f, 40f)]
        [SerializeField] private float _dejectedHeadPitch = 16f;

        [Tooltip("Degrees the neck droops when the reply fails.")]
        [Range(0f, 20f)]
        [SerializeField] private float _dejectedNeckDroop = 7f;

        [Tooltip("Seconds the antenna lamp stays dark after the reply fails, before it relights softly.")]
        [Range(0.5f, 20f)]
        [SerializeField] private float _lampDarkSeconds = 6f;

        [Header("The locked call: the lamp keys the pattern back")]
        [Tooltip("Seconds of a short key-down and of the long one.")]
        [Range(0.34f, 1.5f)]
        [SerializeField] private float _shortSeconds = 0.4f;

        [Range(0.5f, 3f)]
        [SerializeField] private float _longSeconds = 1.2f;

        [Tooltip("Seconds of dark between the keys.")]
        [Range(0.2f, 1f)]
        [SerializeField] private float _gapSeconds = 0.35f;

        [Tooltip("Lamp glow while a key is down (linear fraction of authored).")]
        [Range(0f, 3f)]
        [SerializeField] private float _keyGlow = 1.5f;

        [Tooltip("Seconds after the call locks before 07 first keys the pattern back.")]
        [Range(0f, 10f)]
        [SerializeField] private float _keyDelay = 1.5f;

        [Tooltip("Seconds between the pattern's repeats while the call stays locked and 07 idles.")]
        [Range(4f, 60f)]
        [SerializeField] private float _keyRepeat = 12f;

        [Tooltip("Half-life (s) of the lamp's glow easing to a new level: the soft edge of every key and stutter.")]
        [Range(0.02f, 0.5f)]
        [SerializeField] private float _lampHalfLife = 0.07f;

        public float HeadHalfLife => _headHalfLife;
        public float IdleShare => _idleShare;
        public float StutterBase => _stutterBase;
        public float StutterGlow => _stutterGlow;
        public float StutterMinSeconds => _stutterMinSeconds;
        public float StutterMaxSeconds => Mathf.Max(_stutterMinSeconds, _stutterMaxSeconds);
        public float StutterLitShare => _stutterLitShare;
        public float ReachInterval => _reachInterval;
        public float ReachFirstSeconds => _reachFirstSeconds;
        public float ReachRise => _reachRise;
        public float ReachHold => _reachHold;
        public float ReachFall => _reachFall;
        public float ReachHeadPitch => _reachHeadPitch;
        public float ReachNeckLift => _reachNeckLift;
        public float ReachAntennaLift => _reachAntennaLift;
        public float ShortSeconds => _shortSeconds;
        public float LongSeconds => _longSeconds;
        public float GapSeconds => _gapSeconds;
        public float KeyGlow => _keyGlow;
        public float KeyDelay => _keyDelay;
        public float KeyRepeat => _keyRepeat;
        public float LampHalfLife => _lampHalfLife;
        public float ClearGlow => _clearGlow;
        public float DejectedHoldSeconds => _dejectedHoldSeconds;
        public float DejectedHalfLife => _dejectedHalfLife;
        public float DejectedHeadPitch => _dejectedHeadPitch;
        public float DejectedNeckDroop => _dejectedNeckDroop;
        public float LampDarkSeconds => _lampDarkSeconds;
    }
}
