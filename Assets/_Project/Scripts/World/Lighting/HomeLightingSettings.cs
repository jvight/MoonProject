using System;
using System.Collections.Generic;
using UnityEngine;
using MoonProject.Art;
using MoonProject.Core;

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

        [Header("Light pools")]
        [Tooltip("Colour of the warm light pools on the ground: the lamps' own warm.")]
        [SerializeField] private Color _poolColor = Palette.Get(PaletteSwatch.WarmLamp);

        [Tooltip("The warm pools on home's ground, growing with each stage: the dock while the base sleeps, the porch " +
            "and the windows' spill at Home, the bay's apron at Bay, the deck and the lift's foot at Lift.")]
        [SerializeField] private LightPoolTuning[] _pools =
        {
            new LightPoolTuning(HomeLight.Dock, "DockAnchor", new Vector3(0f, 2.4f, 0.8f), 6.5f, 4f),
            new LightPoolTuning(HomeLight.PorchLamps, "Lander", new Vector3(0f, 3.2f, 4.5f), 11f, 6f),
            new LightPoolTuning(HomeLight.Windows, "Lander", new Vector3(-3.6f, 2.6f, 0.5f), 9f, 4f),
            new LightPoolTuning(HomeLight.BayLamps, "RoverBay", new Vector3(0f, 2.8f, 4f), 10f, 6f),
            new LightPoolTuning(HomeLight.LiftLamps, "LiftTop", new Vector3(0f, 2f, 0f), 6f, 3.5f),
            new LightPoolTuning(HomeLight.LiftLamps, "LiftBottom", new Vector3(0f, 2.4f, 1.4f), 7f, 3.5f),
        };

        public float WakeStagger => _wakeStagger;
        public float WakeFade => _wakeFade;
        public float CableTravel => _cableTravel;
        public float CableSegmentFade => _cableSegmentFade;
        public float PilotPeriod => _pilotPeriod;
        public float PilotSharpness => _pilotSharpness;
        public float PilotIntensity => _pilotIntensity;
        public float PilotRadius => _pilotRadius;
        public Color PoolColor => _poolColor;
        public IReadOnlyList<LightPoolTuning> Pools => _pools;
    }
}
