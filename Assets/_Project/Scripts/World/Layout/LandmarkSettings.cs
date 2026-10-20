using System;
using UnityEngine;
using MoonProject.Core;

namespace MoonProject.World
{
    /// <summary>
    /// Where World stands the opening's places (opening v2 §4): the Lumen station ruins cluster, the landed cargo
    /// lander and its beacon, the old cables, Kestrel-3's debris, the big boulders, the Scout and the west signal.
    /// </summary>
    [Serializable]
    public sealed class LandmarkSettings
    {
        private const string MidwayAnchor = WorldAnchorIds.PlaceMidway;
        private const string KestrelAnchor = WorldAnchorIds.SitePrefix + "kestrel";

        [Header("Lumen station ruins cluster (RUINS-WORLD)")]
        [Tooltip("Pieces of the Lumen station ruins cluster arranged relative to SiteSettings.RuinsCenter: " +
            "habitat left, radio tower behind, workshop right, plus scatter set, with drivable lanes between them; " +
            "the fallen solar array lies off them on its own sunlit footprint (SiteSettings.FallenArrayCenter).")]
        [SerializeField] private RuinPlacement[] _ruins =
        {
            new RuinPlacement("HabitatModules", new Vector2(-5.5f, 2.0f), 10f, 5.8f),
            new RuinPlacement("RadioTower", new Vector2(1.5f, 8.2f), 8f, 3.8f),
            new RuinPlacement("KenjiWorkshop", new Vector2(6.8f, 1.2f), 16f, 3.6f),
            new RuinPlacement("FallenSolarArray", new Vector2(-22f, -24f), 20f, 3.8f),
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

        [Tooltip("The fallen solar array's glint in the new sunlight (opening v3 R4).")]
        [SerializeField] private ArrayGlintSettings _arrayGlint = new ArrayGlintSettings();

        [Tooltip("The habitat's power module status lamp, blinking from dusk (opening v3 R4).")]
        [SerializeField] private ModuleLampSettings _moduleLamp = new ModuleLampSettings();

        [Tooltip("relay.1's lamp, dark until the call is answered and then keying with its sign (opening v3 R8).")]
        [SerializeField] private RelayLampSettings _relayLamp = new RelayLampSettings();

        [Tooltip("relay.1's repair site at the mast's foot: the open cabinet with the spare, the feed socket, the pad " +
            "(opening v3 5.1).")]
        [SerializeField] private Relay1SiteSettings _relay1Site = new Relay1SiteSettings();

        [Tooltip("relay.2's hut by the canyon mouth, 06 slumped against it, and 07's scan spot (opening v3 5.2b).")]
        [SerializeField] private Relay2SiteSettings _relay2Site = new Relay2SiteSettings();

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

        [Header("The Scout, the 1967 soft lander (basin dressing §2, §4)")]
        [Tooltip("Yaw of the Scout, degrees clockwise from +Z: it stands square on its crater's wall, its planar " +
            "array turned to 327.6 degrees, halfway between Earth and the apron, as near a mirror for the earthshine " +
            "as Art's fixed array tilt allows.")]
        [Range(0f, 360f)]
        [SerializeField] private float _scoutYaw = 337.6f;

        [Tooltip("How deep the Scout's footpads sit in the dust, metres: fifty years of settling.")]
        [Range(0f, 0.4f)]
        [SerializeField] private float _scoutSink = 0.08f;

        [Tooltip("How far Art's blast patch sits below the dust line under the Scout, metres: it follows the slope " +
            "and must not float off it.")]
        [Range(0f, 0.2f)]
        [SerializeField] private float _scoutBlastSink = 0.03f;

        [Header("The crew's traces (basin dressing §2)")]
        [Tooltip("Art's crew-trace props (Generated/Art/R1/Models/Props) by the opening's places: the mission flag at " +
            "the ruins' front door, in The Peak's golden-hour shadow; the retroreflector beside trail A on the sunlit " +
            "plain at that shadow's edge, where a ranging target has open sky; the seismic station past the workshop; " +
            "and the survey tripod the last landing knocked over on the blast ridge, legs pointing away from the lander.")]
        [SerializeField] private PropPlacement[] _crewTraces =
        {
            new PropPlacement(WorldAnchorIds.PlaceRuins, "Prop_MissionFlag", new Vector2(-13.6f, 3f), 218f, 0.05f),
            new PropPlacement(WorldAnchorIds.PlaceLander, "Prop_Retroreflector", new Vector2(17.85f, 0.18f), 112f, 0.03f),
            new PropPlacement(WorldAnchorIds.PlaceRuins, "Prop_SeismicStation", new Vector2(32.9f, -2f), 31f, 0.03f),
            new PropPlacement(WorldAnchorIds.PlaceLander, "Prop_SurveyTripod", new Vector2(-37.9f, -0.2f), 180f, 0.05f),
        };

        [Tooltip("Where the sample rack stands from the Scout crater's centre (XZ, metres): on its north rim, where " +
            "the 1971 crew cored the crater.")]
        [SerializeField] private Vector2 _sampleRackOffset = new Vector2(0f, 10f);

        [Tooltip("Yaw of the sample rack, degrees clockwise from +Z: its fallen core tube lies down the rim's outer " +
            "slope, open end out.")]
        [Range(0f, 360f)]
        [SerializeField] private float _sampleRackYaw = 225f;

        [Tooltip("Height above the apron of the eye the retroreflector's golden-hour glint is aimed at, metres: about " +
            "07's chase camera. The panel turns on its hinge halfway between the Sun and that eye.")]
        [Range(0.5f, 10f)]
        [SerializeField] private float _reflectorEyeHeight = 3.2f;

        [Tooltip("Bootprint trail A (world XZ waypoints, metres): the crew's walk from the apron's edge, round the " +
            "lander's footprint, to the ruins' front door between the flag and the retroreflector.")]
        [SerializeField] private Vector2[] _bootprintTrail =
        {
            new Vector2(27f, 147f),
            new Vector2(42.7f, 148.6f),
            new Vector2(79.5f, 185.5f),
        };

        [Tooltip("How far each bootprint tile wanders off the trail's line, metres: a walked path, not a ruler.")]
        [Range(0f, 1f)]
        [SerializeField] private float _bootprintWander = 0.3f;

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

        [Header("The databank's story scan hosts (docs/design/text/databank-entries.md)")]
        [Tooltip("Kestrel-3's hull panel at the impact crater, the host of its story scan (entry 6): one of Art's " +
            "debris pieces (Generated/Art/R1/Models/Wreck), stood in [World]/BasinProps under its own host name.")]
        [SerializeField] private PropPlacement _kestrelPanel =
            new PropPlacement(KestrelAnchor, "Kestrel_HullPanel", new Vector2(-2.2f, -1.4f), 160f, 0.3f);

        [Tooltip("06, the SSR-3 relay keeper, stopped at micro.06 by the old canyon-mouth mast (entry 2): Art's " +
            "Elder06 (Generated/Art/R1/Models/Rover). It faces the canyon mouth, where relay.2 stood; its yaw turns " +
            "it from there, degrees clockwise.")]
        [SerializeField] private PropPlacement _elder06 =
            new PropPlacement(WorldAnchorIds.Micro06, "Elder06", Vector2.zero, 0f, 0.08f);

        [Tooltip("How far 06 slumps over onto its left side, degrees: its cell ran dry and it settled where a wheel " +
            "sank into the dust, its top come to rest against relay.2's hut.")]
        [Range(0f, 20f)]
        [SerializeField] private float _elder06Slump = 9f;

        [Tooltip("01, the SSR-3 survey cart, stopped by a rockfall where the Peak road leaves the basin (entry 3): " +
            "Art's Elder01, turned a little off the road so its mast reads from the drive up.")]
        [SerializeField] private PropPlacement _elder01 =
            new PropPlacement(WorldAnchors.PeakRoad, "Elder01", Vector2.zero, 30f, 0f);

        [Tooltip("The rockfall that stopped 01: Art's boulders across the Peak road a few metres ahead of it, settled " +
            "in the dust.")]
        [SerializeField] private PropPlacement[] _rockfall =
        {
            new PropPlacement(WorldAnchors.PeakRoad, "Rock_09", new Vector2(0.4f, 6.6f), 70f, 0.5f),
            new PropPlacement(WorldAnchors.PeakRoad, "Rock_08", new Vector2(3.7f, 4.8f), 205f, 0.4f),
            new PropPlacement(WorldAnchors.PeakRoad, "Rock_08", new Vector2(-2f, 9.1f), 320f, 0.45f),
        };

        public RuinPlacement[] Ruins => _ruins;
        public float LanderTilt => _landerTilt;
        public GroundCableSettings WakeCable => _wakeCable;
        public LanderBeaconSettings LanderBeacon => _landerBeacon;
        public ArrayGlintSettings ArrayGlint => _arrayGlint;
        public ModuleLampSettings ModuleLamp => _moduleLamp;

        public RelayLampSettings RelayLamp => _relayLamp;
        public Relay1SiteSettings Relay1Site => _relay1Site;
        public Relay2SiteSettings Relay2Site => _relay2Site;
        public PropPlacement[] Debris => _debris;
        public PropPlacement[] Boulders => _boulders;
        public float ScoutYaw => _scoutYaw;
        public float ScoutSink => _scoutSink;
        public float ScoutBlastSink => _scoutBlastSink;
        public PropPlacement[] CrewTraces => _crewTraces;
        public Vector2 SampleRackOffset => _sampleRackOffset;
        public float SampleRackYaw => _sampleRackYaw;
        public float ReflectorEyeHeight => _reflectorEyeHeight;
        public Vector2[] BootprintTrail => _bootprintTrail;
        public float BootprintWander => _bootprintWander;
        public float SignalPillarSetback => _signalPillarSetback;
        public float SignalPillarGlow => _signalPillarGlow;
        public float SignalPillarWidth => _signalPillarWidth;
        public PropPlacement KestrelPanel => _kestrelPanel;
        public PropPlacement Elder06 => _elder06;
        public float Elder06Slump => _elder06Slump;
        public PropPlacement Elder01 => _elder01;
        public PropPlacement[] Rockfall => _rockfall;
    }
}
