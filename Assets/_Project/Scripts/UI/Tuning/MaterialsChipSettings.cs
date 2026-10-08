using System;
using UnityEngine;

namespace MoonProject.UI
{
    /// <summary>
    /// The materials chip: no permanent HUD; it drifts in when 07's metal, wiring or optics change, counts each to its
    /// new amount, lets the material that grew glow softly, lingers and leaves.
    /// </summary>
    [Serializable]
    public sealed class MaterialsChipSettings
    {
        [Tooltip("The chip easing in and out.")]
        [SerializeField] private RevealSettings _reveal = new RevealSettings(0.35f, 0.7f, -10f, 0.92f, 1.8f, 0.6f);

        [Tooltip("Seconds of counting per unit of change (bounded by the min and max below).")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _secondsPerUnit = 0.12f;

        [Tooltip("Shortest count, even for a single unit.")]
        [Range(0f, 2f)]
        [SerializeField] private float _minCountSeconds = 0.4f;

        [Tooltip("Longest count, even for a big haul.")]
        [Range(0.1f, 5f)]
        [SerializeField] private float _maxCountSeconds = 1.4f;

        [Tooltip("Seconds the chip lingers after every count settles before it fades away.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _lingerSeconds = 3f;

        [Tooltip("Seconds a material that grew glows: a soft swell and settle, never a flash.")]
        [Range(0.1f, 4f)]
        [SerializeField] private float _pulseSeconds = 1.2f;

        [Tooltip("How much the icon of a material that grew swells at the top of its glow (0.1 = 10 % larger).")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _pulseScale = 0.16f;

        [Tooltip("Opacity of the soft halo behind that icon at the top of its glow.")]
        [Range(0f, 1f)]
        [SerializeField] private float _glowOpacity = 0.35f;

        public RevealSettings Reveal => _reveal;

        public float SecondsPerUnit => _secondsPerUnit;

        public float MinCountSeconds => _minCountSeconds;

        public float MaxCountSeconds => _maxCountSeconds;

        public float LingerSeconds => _lingerSeconds;

        public float PulseSeconds => _pulseSeconds;

        public float PulseScale => _pulseScale;

        public float GlowOpacity => _glowOpacity;
    }
}
