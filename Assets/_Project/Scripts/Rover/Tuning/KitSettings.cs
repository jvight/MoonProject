using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's visible kit and the friends' gifts (VISION ruling 11, docs/features/M3-11): how a crafted piece grows as
    /// the Rover Bay's arm carries it in, how a gift appears (a soft grow), how the Warm Headlamp's lamp bar changes
    /// the road light, how the capacitor drums glow with the boost, and the install moment in the bay (M3-14).
    /// </summary>
    [Serializable]
    public sealed class KitSettings
    {
        [Header("Fitting a kit piece")]
        [Tooltip("Spring frequency (Hz) of a crafted piece growing to full size as the bay's arm carries it in.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _fitGrowFrequency = 3f;

        [Tooltip("Half-life (s) of a fitted piece's lights coming on (the lamp bar's glasses, the warmer road light).")]
        [Range(0.05f, 3f)]
        [SerializeField] private float _lightsOnHalfLife = 0.35f;

        [Header("The install moment in the Rover Bay")]
        [SerializeField] private BayFitSettings _bay = new BayFitSettings();

        [Header("A friend's gift")]
        [Tooltip("Seconds after the soft moment starts before the gift appears.")]
        [Range(0f, 3f)]
        [SerializeField] private float _giftDelay = 0.9f;

        [Tooltip("Spring frequency (Hz) of a gift growing into place from nothing.")]
        [Range(0.3f, 6f)]
        [SerializeField] private float _giftFrequency = 1.4f;

        [Tooltip("Damping ratio of that grow: below 1 it grows a hair past full size and settles.")]
        [Range(0.2f, 1.5f)]
        [SerializeField] private float _giftDamping = 0.6f;

        [Tooltip("07 counts as home (a pending gift appears) within this distance (m) of the base.")]
        [Range(5f, 100f)]
        [SerializeField] private float _giftHomeRadius = 22f;

        [Tooltip("...and slower than this (m/s), so the gift arrives as 07 settles in, not as it rushes past.")]
        [Range(0.1f, 10f)]
        [SerializeField] private float _giftMaxSpeed = 2.5f;

        [Header("Warm Headlamp (the lamp bar)")]
        [Tooltip("Glow of the lamp bar's three glasses once on (linear; below 07's eye so they never compete).")]
        [Range(0f, 3f)]
        [SerializeField] private float _lampBarGlow = 0.7f;

        [Tooltip("Road light intensity with the lamp bar.")]
        [Range(0f, 200f)]
        [SerializeField] private float _warmIntensity = 42f;

        [Tooltip("Road light range (m) with the lamp bar.")]
        [Range(1f, 60f)]
        [SerializeField] private float _warmRange = 24f;

        [Tooltip("Road light outer cone angle (deg) with the lamp bar: a wider pool.")]
        [Range(10f, 150f)]
        [SerializeField] private float _warmSpotAngle = 90f;

        [Tooltip("Road light inner (full brightness) cone angle (deg) with the lamp bar.")]
        [Range(1f, 150f)]
        [SerializeField] private float _warmInnerSpotAngle = 48f;

        [Tooltip("Road light colour with the lamp bar: the lamp's amber nudged toward 07's warm orange.")]
        [SerializeField] private Color _warmColor = new Color(1f, 0.62f, 0.33f);

        [Header("Boost Coils (the capacitor drums)")]
        [Tooltip("Glow of the drums' bands at a full boost (linear; cyan like the Hover-Jump coils).")]
        [Range(0f, 4f)]
        [SerializeField] private float _drumGlow = 1.4f;

        [Tooltip("Faint glow of the bands at rest once fitted, so the drums read as live kit.")]
        [Range(0f, 1f)]
        [SerializeField] private float _drumIdleGlow = 0.05f;

        public float FitGrowFrequency => _fitGrowFrequency;

        public BayFitSettings Bay => _bay;

        public float LightsOnHalfLife => _lightsOnHalfLife;

        public float GiftDelay => _giftDelay;

        public float GiftFrequency => _giftFrequency;

        public float GiftDamping => _giftDamping;

        public float GiftHomeRadius => _giftHomeRadius;

        public float GiftMaxSpeed => _giftMaxSpeed;

        public float LampBarGlow => _lampBarGlow;

        public float WarmIntensity => _warmIntensity;

        public float WarmRange => _warmRange;

        public float WarmSpotAngle => _warmSpotAngle;

        public float WarmInnerSpotAngle => Mathf.Min(_warmInnerSpotAngle, _warmSpotAngle);

        public Color WarmColor => _warmColor;

        public float DrumGlow => _drumGlow;

        public float DrumIdleGlow => _drumIdleGlow;
    }
}
