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

        [Tooltip("Seconds power takes to travel a ground cable from the tower to what it wakes, segment by segment; " +
            "that place's warm points start waking when it arrives.")]
        [Range(0.1f, 6f)]
        [SerializeField] private float _cableTravel = 1.5f;

        [Tooltip("Seconds each cable segment takes to ease from dark to lit.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _cableSegmentFade = 0.35f;

        [Tooltip("Seconds between blinks of the tower's pilot light while the base is asleep. Slow: a heartbeat " +
            "saying something here still waits.")]
        [Range(1f, 10f)]
        [SerializeField] private float _pilotPeriod = 3.5f;

        [Tooltip("How short each pilot blink is: higher = a briefer flash in a longer dark.")]
        [Range(1f, 16f)]
        [SerializeField] private float _pilotSharpness = 6f;

        [Tooltip("Halo intensity of the pilot light at the top of a blink (LofiBeaconHalo _Intensity).")]
        [Range(0f, 4f)]
        [SerializeField] private float _pilotIntensity = 1.4f;

        [Tooltip("Radius of the pilot light's halo, metres.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _pilotRadius = 0.35f;

        public float WakeStagger => _wakeStagger;
        public float WakeFade => _wakeFade;
        public float CableTravel => _cableTravel;
        public float CableSegmentFade => _cableSegmentFade;
        public float PilotPeriod => _pilotPeriod;
        public float PilotSharpness => _pilotSharpness;
        public float PilotIntensity => _pilotIntensity;
        public float PilotRadius => _pilotRadius;
    }
}
