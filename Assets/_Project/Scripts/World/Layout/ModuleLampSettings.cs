using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The Lumen habitat's power module status lamp, on the towable module (opening v3 R4): dark by day, a slow, soft amber blink from dusk
    /// through the night, so the one thing in the ruins still holding a trickle of charge calls 07 over in the dark.
    /// </summary>
    [Serializable]
    public sealed class ModuleLampSettings
    {
        [Tooltip("Tow id of the power module whose lamp blinks: the towable module Gameplay stands beside the habitat " +
            "(the same id as in OpeningTuning).")]
        [SerializeField] private string _towId = "habitat.power_module";

        [Tooltip("Seconds from one blink to the next: slow, a sleeping machine's pulse, never an alarm.")]
        [Range(1f, 8f)]
        [SerializeField] private float _blinkSeconds = 3.2f;

        [Tooltip("Share of each blink the lamp is lit, its soft rise and fall included.")]
        [Range(0.05f, 0.8f)]
        [SerializeField] private float _onShare = 0.3f;

        [Tooltip("Share of the lit time spent easing up and easing down (0 a hard flash, 1 all ease).")]
        [Range(0f, 1f)]
        [SerializeField] private float _easeShare = 0.6f;

        [Tooltip("Seconds the blink takes to wake once dusk falls (and to sleep again at sunrise).")]
        [Range(0.2f, 20f)]
        [SerializeField] private float _wakeSeconds = 4f;

        [Tooltip("Emission strength of the lamp's own lens at the top of a blink.")]
        [Range(0f, 6f)]
        [SerializeField] private float _lampGlow = 2.2f;

        [Tooltip("Colour of the lamp's halo: the lens's warm amber.")]
        [SerializeField] private Color _haloColor = new Color(1f, 0.68f, 0.28f, 1f);

        [Tooltip("Brightness of the lamp's halo at the top of a blink.")]
        [Range(0f, 6f)]
        [SerializeField] private float _haloIntensity = 2f;

        [Tooltip("Radius of the lamp's halo up close, metres: a tight glow round a 7 cm lens.")]
        [Range(0.02f, 1f)]
        [SerializeField] private float _haloRadius = 0.12f;

        [Tooltip("Smallest angular size of the halo, degrees: a point that still reads from the base at night.")]
        [Range(0f, 2f)]
        [SerializeField] private float _haloMinAngle = 0.25f;

        [Tooltip("How far the halo floats out in front of the lens, metres, so the bulkhead never cuts it.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _haloLift = 0.08f;

        [Header("Its purpose")]
        [Tooltip("How far from the habitat ruin the module still counts as waiting there, metres: inside it, unhooked, " +
            "the lamp is a blinking lure; anywhere else (on the cable, home, built into the bank) it glows steady.")]
        [Range(2f, 40f)]
        [SerializeField] private float _lureRadius = 12f;

        [Tooltip("Share of the blink's peak the lamp holds once its job is done: a calm, steady 'powered' glow.")]
        [Range(0f, 1f)]
        [SerializeField] private float _poweredLevel = 0.55f;

        [Tooltip("Seconds the lamp takes to ease from the lure's blink to the steady glow.")]
        [Range(0.1f, 6f)]
        [SerializeField] private float _purposeFadeSeconds = 1.5f;

        public string TowId => _towId;
        public float LureRadius => _lureRadius;
        public float PoweredLevel => _poweredLevel;
        public float PurposeFadeSeconds => _purposeFadeSeconds;
        public float BlinkSeconds => _blinkSeconds;
        public float OnShare => _onShare;
        public float EaseShare => _easeShare;
        public float WakeSeconds => _wakeSeconds;
        public float LampGlow => _lampGlow;
        public Color HaloColor => _haloColor;
        public float HaloIntensity => _haloIntensity;
        public float HaloRadius => _haloRadius;
        public float HaloMinAngle => _haloMinAngle;
        public float HaloLift => _haloLift;
    }
}
