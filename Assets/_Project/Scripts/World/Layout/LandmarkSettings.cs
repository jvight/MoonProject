using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// Where World stands the opening's places (opening v2 §4): the Lumen station ruins cluster, the landed cargo
    /// lander and its beacon, the old cables, Kestrel-3's debris, the big boulders and the west signal.
    /// </summary>
    [Serializable]
    public sealed class LandmarkSettings
    {
        private const string MidwayAnchor = WorldAnchorIds.PlaceMidway;
        private const string KestrelAnchor = WorldAnchorIds.SitePrefix + "kestrel";

        [Header("Lumen station ruins cluster (RUINS-WORLD)")]
        [Tooltip("Pieces of the Lumen station ruins cluster arranged relative to SiteSettings.RuinsCenter: " +
            "habitat left, radio tower behind, workshop right, fallen solar array in front, plus scatter set, " +
            "with drivable lanes between them.")]
        [SerializeField] private RuinPlacement[] _ruins =
        {
            new RuinPlacement("HabitatModules", new Vector2(-5.5f, 2.0f), 10f, 5.8f),
            new RuinPlacement("RadioTower", new Vector2(1.5f, 8.2f), 8f, 3.8f),
            new RuinPlacement("KenjiWorkshop", new Vector2(6.8f, 1.2f), 16f, 3.6f),
            new RuinPlacement("FallenSolarArray", new Vector2(2.2f, -5.8f), 20f, 3.8f),
            new RuinPlacement("ScatterSet", new Vector2(-5.2f, -5.2f), -18f, 2.6f),
        };

        [Header("The landed cargo lander (opening v2 §4)")]
        [Tooltip("How far the landed lander leans off level, degrees, about the axis from its port side to the far " +
            "side: a hard landing on uneven ground, slightly tilted.")]
        [Range(0f, 8f)]
        [SerializeField] private float _landerTilt = 2.5f;

        [Tooltip("The old power cables: 07's from its dead post to the lander's port, and the port's to the apron.")]
        [SerializeField] private GroundCableSettings _wakeCable = new GroundCableSettings();

        [Tooltip("The lander's amber emergency beacon on top of its hull.")]
        [SerializeField] private LanderBeaconSettings _landerBeacon = new LanderBeaconSettings();

        [Header("Kestrel-3's debris and the big boulders (opening v2 §4)")]
        [Tooltip("Art's R1 Kestrel-3 debris (Generated/Art/R1/Models/Wreck): a few pieces at the first drive's midway " +
            "point and more round the impact crater, settled in the dust.")]
        [SerializeField] private PropPlacement[] _debris =
        {
            new PropPlacement(MidwayAnchor, "Kestrel_HullPanel", new Vector2(-1.6f, 1.4f), 30f, 0.25f),
            new PropPlacement(MidwayAnchor, "Kestrel_StrutFrame", new Vector2(2.2f, -0.6f), 115f, 0.15f),
            new PropPlacement(MidwayAnchor, "Kestrel_CableTray", new Vector2(0.3f, -2.8f), 205f, 0.1f),
            new PropPlacement(KestrelAnchor, "Kestrel_Tank", new Vector2(3.2f, 1.8f), 15f, 0.3f),
            new PropPlacement(KestrelAnchor, "Kestrel_EngineBell", new Vector2(-3.4f, 2.2f), 250f, 0.4f),
            new PropPlacement(KestrelAnchor, "Kestrel_LandingFoot", new Vector2(0.6f, -4.2f), 80f, 0.2f),
            new PropPlacement(KestrelAnchor, "Kestrel_HullPanel", new Vector2(-2.2f, -1.4f), 160f, 0.3f),
        };

        [Tooltip("Art's big R1 boulders (Rock_10..13, 3-8 m) hand-placed near the opening's routes as landmarks and " +
            "cover, off the lanes and the footprints.")]
        [SerializeField] private PropPlacement[] _boulders =
        {
            new PropPlacement(MidwayAnchor, "Rock_12", new Vector2(24f, 14f), 40f, 0.8f),
            new PropPlacement(WorldAnchorIds.PlaceWake, "Rock_11", new Vector2(-30f, 46f), 120f, 0.6f),
            new PropPlacement(WorldAnchorIds.PlaceApron, "Rock_10", new Vector2(-26f, 4f), 210f, 0.5f),
            new PropPlacement(WorldAnchorIds.PlaceLander, "Rock_13", new Vector2(-30f, 26f), 300f, 1f),
            new PropPlacement(WorldAnchorIds.PlaceRuins, "Rock_12", new Vector2(10f, 30f), 75f, 0.8f),
        };

        [Header("The west signal (opening v2 B11)")]
        [Tooltip("How far behind the dead west mast (away from the basin) the signal pillar rises, metres; it stands " +
            "hidden until Gameplay shows it when the antenna mast hears the west.")]
        [Range(0f, 80f)]
        [SerializeField] private float _signalPillarSetback = 18f;

        [Tooltip("Emission strength of the signal pillar's beam once shown (R1Lit _EmissionStrength): its HDR core " +
            "reads from the base across the night basin.")]
        [Range(0f, 4f)]
        [SerializeField] private float _signalPillarGlow = 3.5f;

        [Tooltip("How much wider than Art's beam the pillar stands (times), so it reads at the rim, 300 m out.")]
        [Range(1f, 8f)]
        [SerializeField] private float _signalPillarWidth = 3f;

        public RuinPlacement[] Ruins => _ruins;
        public float LanderTilt => _landerTilt;
        public GroundCableSettings WakeCable => _wakeCable;
        public LanderBeaconSettings LanderBeacon => _landerBeacon;
        public PropPlacement[] Debris => _debris;
        public PropPlacement[] Boulders => _boulders;
        public float SignalPillarSetback => _signalPillarSetback;
        public float SignalPillarGlow => _signalPillarGlow;
        public float SignalPillarWidth => _signalPillarWidth;
    }
}
