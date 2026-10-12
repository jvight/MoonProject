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

        [Tooltip("Seconds between one unit and the next as a count rolls (the count never jumps).")]
        [Range(0.02f, 0.3f)]
        [SerializeField] private float _stepSeconds = 0.08f;

        [Tooltip("Longest roll, even for a big haul: a bigger change steps quicker to stay within it.")]
        [Range(0.1f, 5f)]
        [SerializeField] private float _maxCountSeconds = 1.4f;

        [Tooltip("Seconds the chip lingers after every count settles before it fades away.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _lingerSeconds = 3f;

        [Tooltip("Seconds a material that grew glows: a soft swell and settle, never a flash.")]
        [Range(0.1f, 4f)]
        [SerializeField] private float _pulseSeconds = 1.2f;

        [Tooltip("Seconds of the small pulse each unit of a rising count gives its icon.")]
        [Range(0.05f, 0.5f)]
        [SerializeField] private float _tickPulseSeconds = 0.14f;

        [Tooltip("How much that icon swells at the top of a unit's pulse (0.1 = 10 % larger).")]
        [Range(0f, 0.3f)]
        [SerializeField] private float _tickPulseScale = 0.08f;

        [Tooltip("How much the icon of a material that grew swells at the top of its glow (0.1 = 10 % larger).")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _pulseScale = 0.16f;

        [Tooltip("Opacity of the soft halo behind that icon at the top of its glow.")]
        [Range(0f, 1f)]
        [SerializeField] private float _glowOpacity = 0.35f;

        public RevealSettings Reveal => _reveal;

        public float StepSeconds => _stepSeconds;

        public float MaxCountSeconds => _maxCountSeconds;

        public float LingerSeconds => _lingerSeconds;

        public float PulseSeconds => _pulseSeconds;

        public float TickPulseSeconds => _tickPulseSeconds;

        public float TickPulseScale => _tickPulseScale;

        public float PulseScale => _pulseScale;

        public float GlowOpacity => _glowOpacity;
    }
}
