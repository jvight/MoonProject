using System;
using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's visible kit and the friends' gifts (VISION ruling 11, docs/features/M3-11): how a newly fitted piece comes
    /// in (it appears just above its socket, drops and settles with a small overshoot), how a gift appears (a soft
    /// grow), how the Warm Headlamp's lamp bar changes the road light, and how the capacitor drums glow with the boost.
    /// </summary>
    [Serializable]
    public sealed class KitSettings
    {
        [Header("Fitting a kit piece")]
        [Tooltip("Seconds after a purchase before the piece appears, so the camera has eased round to look first.")]
        [Range(0f, 3f)]
        [SerializeField] private float _fitDelay = 1f;

        [Tooltip("Height (m) above its socket where a newly fitted piece appears before it drops into place.")]
        [Range(0f, 1f)]
        [SerializeField] private float _fitDrop = 0.32f;

        [Tooltip("Spring frequency (Hz) of the drop into place.")]
        [Range(0.3f, 6f)]
        [SerializeField] private float _fitFrequency = 1.6f;

        [Tooltip("Damping ratio of the drop: below 1 it dips once just past its seat and settles (a small overshoot).")]
        [Range(0.2f, 1.5f)]
        [SerializeField] private float _fitDamping = 0.55f;

        [Tooltip("Scale a newly fitted piece appears at, growing to 1 as it drops (never a hard pop-in).")]
        [Range(0f, 1f)]
        [SerializeField] private float _fitStartScale = 0.35f;

        [Tooltip("Spring frequency (Hz) of that grow.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _fitGrowFrequency = 3f;

        [Tooltip("Half-life (s) of a fitted piece's lights coming on (the lamp bar's glasses, the warmer road light).")]
        [Range(0.05f, 3f)]
        [SerializeField] private float _lightsOnHalfLife = 0.35f;

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

        public float FitDelay => _fitDelay;

        public float FitDrop => _fitDrop;

        public float FitFrequency => _fitFrequency;

        public float FitDamping => _fitDamping;

        public float FitStartScale => _fitStartScale;

        public float FitGrowFrequency => _fitGrowFrequency;

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
