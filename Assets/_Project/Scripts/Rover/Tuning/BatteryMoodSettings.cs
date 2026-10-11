using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// How 07 carries its battery (docs/features/M3-20 "How 07 shows it"): below the Low share the eye dims a little,
    /// the head bows and the wing stays flatter; crawling on the reserve it is quieter still; a charger perks it up; a
    /// refused leap gets a glance at its own lamps and the VeryLow warning a look toward the nearest lit charger. Tired,
    /// never sad.
    /// </summary>
    [Serializable]
    public sealed class BatteryMoodSettings
    {
        [Tooltip("Below this share of charge 07 looks tired (the battery's Low warning share).")]
        [Range(0f, 1f)]
        [SerializeField] private float _lowShare = 0.3f;

        [Tooltip("Eye glow (multiplier) once tired.")]
        [Range(0.2f, 1f)]
        [SerializeField] private float _lowEyeGlow = 0.8f;

        [Tooltip("Eye glow (multiplier, on top of the tired one) while crawling on the reserve.")]
        [Range(0.2f, 1f)]
        [SerializeField] private float _crawlEyeGlow = 0.65f;

        [Tooltip("Head bow (deg) once tired.")]
        [Range(0f, 15f)]
        [SerializeField] private float _lowHeadBow = 3f;

        [Tooltip("Further head bow (deg) while crawling on the reserve.")]
        [Range(0f, 20f)]
        [SerializeField] private float _crawlHeadBow = 6f;

        [Tooltip("Share of its usual opening the solar wing settles to once tired (it droops flatter).")]
        [Range(0f, 1f)]
        [SerializeField] private float _lowWingOpen = 0.4f;

        [Tooltip("Half-life (s) of the tired look and the crawl look easing in and out.")]
        [Range(0.05f, 5f)]
        [SerializeField] private float _halfLife = 0.9f;

        [Tooltip("Perk-up strength as a charger starts filling the cells (head lift, brighter eye, antenna wiggle).")]
        [Range(0f, 1f)]
        [SerializeField] private float _chargePerk = 0.7f;

        [Tooltip("Seconds 07 glances at its flank lamps when a leap is refused.")]
        [Range(0.2f, 5f)]
        [SerializeField] private float _lampGlanceSeconds = 1.2f;

        [Tooltip("Seconds 07 looks toward the nearest lit charger at the VeryLow warning.")]
        [Range(0.5f, 8f)]
        [SerializeField] private float _chargerGlanceSeconds = 2.5f;

        [Tooltip("Height (m) above the charger's pad 07 looks at.")]
        [Range(0f, 10f)]
        [SerializeField] private float _chargerGlanceLift = 2f;

        public float LowShare => _lowShare;
        public float LowEyeGlow => _lowEyeGlow;
        public float CrawlEyeGlow => _crawlEyeGlow;
        public float LowHeadBow => _lowHeadBow;
        public float CrawlHeadBow => _crawlHeadBow;
        public float LowWingOpen => _lowWingOpen;
        public float HalfLife => _halfLife;
        public float ChargePerk => _chargePerk;
        public float LampGlanceSeconds => _lampGlanceSeconds;
        public float ChargerGlanceSeconds => _chargerGlanceSeconds;
        public float ChargerGlanceLift => _chargerGlanceLift;
    }
}
