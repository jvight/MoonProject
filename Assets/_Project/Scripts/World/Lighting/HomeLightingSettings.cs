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

        [Tooltip("Steady level of the tower's top light once home is awake (0..1 of the pilot intensity): home " +
            "stays a warm dot from across the basin.")]
        [Range(0f, 1f)]
        [SerializeField] private float _topLightLevel = 0.6f;

        [Tooltip("Smallest apparent radius of the tower's top light, degrees (the Peak beacon's trick), so home " +
            "never shrinks below a readable warm dot at 300 m.")]
        [Range(0f, 2f)]
        [SerializeField] private float _pilotMinAngle = 0.4f;

        [Tooltip("Halo intensity of the pilot light at the top of a blink (LofiBeaconHalo _Intensity).")]
        [Range(0f, 4f)]
        [SerializeField] private float _pilotIntensity = 1.4f;

        [Tooltip("Radius of the pilot light's halo, metres.")]
        [Range(0.05f, 2f)]
        [SerializeField] private float _pilotRadius = 0.12f;

        [Tooltip("Distance (metres) from which the top light's minimum-size halo starts to fade in. Closer, it is a " +
            "small lamp with a tight glow on its housing.")]
        [Range(0f, 300f)]
        [SerializeField] private float _pilotHaloFrom = 60f;

        [Tooltip("Distance (metres) from which the top light keeps its full minimum size: a warm dot for home.")]
        [Range(1f, 400f)]
        [SerializeField] private float _pilotHaloFull = 80f;

        [Header("Light pools")]
        [Tooltip("Colour of the warm light pools on the ground: the lamps' own warm.")]
        [SerializeField] private Color _poolColor = Palette.Get(PaletteSwatch.WarmLamp);

        [Tooltip("The warm pools on home's ground, growing with each stage: the dock while the base sleeps; at Home " +
            "the way in under the belly, the lander's own floor, the belly lift's spill, the lab inside and the " +
            "tower's foot; the bay's apron at Bay; the chart's glow through the big porthole at Chart (M3-19: warm " +
            "pools round the three masses).")]
        [SerializeField] private LightPoolTuning[] _pools =
        {
            new LightPoolTuning(HomeLight.Dock, "DockAnchor", new Vector3(0f, -0.02f, 0.8f), 5f, 1f, true),
            new LightPoolTuning(HomeLight.PorchLamps, "Lander", new Vector3(0f, 2.2f, 5.6f), 9f, 4f, false),
            new LightPoolTuning(HomeLight.Windows, "Lander", new Vector3(0f, 2.9f, 0f), 10f, 3f, false),
            new LightPoolTuning(HomeLight.BayLamps, "RoverBay", new Vector3(0f, 0.03f, 4f), 7f, 1f, true),
            new LightPoolTuning(HomeLight.LiftLamps, "BellyLift", new Vector3(0f, 2.25f, 1.2f), 5.5f, 3.5f, false),
            new LightPoolTuning(HomeLight.LabLamps, "Lab", new Vector3(0f, 1.9f, 0f), 4.5f, 2.2f, false),
            new LightPoolTuning(HomeLight.ChartTable, "BigPorthole", new Vector3(-3.3f, 3.1f, -3.3f), 6f, 1.6f,
                false),
            new LightPoolTuning(HomeLight.PorchLamps, "TowerAnchor", new Vector3(0f, 2.6f, 2.2f), 7.5f, 2.6f, false),
        };

        public float WakeStagger => _wakeStagger;
        public float WakeFade => _wakeFade;
        public float CableTravel => _cableTravel;
        public float CableSegmentFade => _cableSegmentFade;
        public float PilotPeriod => _pilotPeriod;
        public float PilotSharpness => _pilotSharpness;
        public float PilotIntensity => _pilotIntensity;
        public float PilotRadius => _pilotRadius;
        public float TopLightLevel => _topLightLevel;
        public float PilotMinAngle => _pilotMinAngle;
        public float PilotHaloFrom => _pilotHaloFrom;
        public float PilotHaloFull => _pilotHaloFull;
        public Color PoolColor => _poolColor;
        public IReadOnlyList<LightPoolTuning> Pools => _pools;
    }
}
