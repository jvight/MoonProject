using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// How home's warm points wake when power reaches their stage (docs/features/M3-16, pillar 6): one by one, each
    /// easing up slowly, so the base comes alive like a house at dusk rather than switching on.
    /// </summary>
    [Serializable]
    public sealed class HomeLightingSettings
    {
        [Tooltip("Seconds between one warm point of a stage starting to wake and the next (windows, then porch " +
            "lamps, then the halo).")]
        [Range(0f, 5f)]
        [SerializeField] private float _wakeStagger = 1.2f;

        [Tooltip("Seconds each warm point takes to ease from dark to fully lit.")]
        [Range(0.1f, 8f)]
        [SerializeField] private float _wakeFade = 2.4f;

        public float WakeStagger => _wakeStagger;
        public float WakeFade => _wakeFade;
    }
}
