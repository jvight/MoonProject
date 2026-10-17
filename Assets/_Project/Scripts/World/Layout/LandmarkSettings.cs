using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Where World places the crew's remnants from Art's landmark kit (M3-18): the route posts along the road to the
    /// canyon (L2), the broken cable bridge over the chasm (L4) and the toppled dish frames of the antenna farm (L7).
    /// </summary>
    [Serializable]
    public sealed class LandmarkSettings
    {
        [Header("Route posts (L2)")]
        [Tooltip("Bearing of the crew's road to the canyon mouth, degrees clockwise from +Z.")]
        [Range(0f, 360f)]
        [SerializeField] private float _routeBearing = 72f;

        [Tooltip("Distances of the route posts from home, metres: about every 45 m from the pad's edge to the mouth.")]
        [SerializeField] private float[] _routePostDistances = { 70f, 115f, 160f, 205f, 250f, 275f };

        [Tooltip("How far the posts stand to the right of the road's centre line, metres, so 07's ruts run past them.")]
        [Range(0f, 10f)]
        [SerializeField] private float _routePostSide = 2.6f;

        [Tooltip("Glow of a post's reflector (linear glow multiplier): a faint glint, never a lamp.")]
        [Range(0f, 2f)]
        [SerializeField] private float _reflectorGlow = 0.3f;

        [Header("Chasm bridge (L4)")]
        [Tooltip("Glow of the lamp on the bridge's far anchor (linear glow multiplier): a warm point across the gap.")]
        [Range(0f, 3f)]
        [SerializeField] private float _bridgeLampGlow = 1f;

        [Tooltip("How far the bridge's anchors stand to the side of the leap line, metres, clear of 07's lane.")]
        [Range(0f, 10f)]
        [SerializeField] private float _bridgeSide = 3.5f;

        [Header("Antenna farm (L7)")]
        [Tooltip("How far the toppled dish frames lie from the farm's centre, metres.")]
        [Range(0f, 20f)]
        [SerializeField] private float _dishSpread = 6.5f;

        [Header("Orange vent (M3-27)")]
        [Tooltip("Soft orange patches in the old vent's bead field (Apollo 17's orange soil), gathered round the vent.")]
        [Range(0, 80)]
        [SerializeField] private int _ventPatches = 26;

        [Tooltip("Radius range of one patch, metres (x = min, y = max).")]
        [SerializeField] private Vector2 _ventPatchRadius = new Vector2(1.2f, 3.6f);

        [Tooltip("How far the patches spread from the vent, as a share of its anchor radius: thick at the vent, " +
            "thinning out.")]
        [Range(0.5f, 3f)]
        [SerializeField] private float _ventSpread = 1.6f;

        [Tooltip("Tint the bead patches multiply the dust by at full strength: a rusty ochre (Apollo 17's orange " +
            "glass). HDR: red above 1 and blue below keep the patch about as bright as the dust, so the grade's cool " +
            "shadow toning never turns it red-violet.")]
        [ColorUsage(false, true)]
        [SerializeField] private Color _ventTint = new Color(1.24f, 1f, 0.66f, 1f);

        [Tooltip("Strength of the patches at the vent (0..1); they fade toward the field's edge.")]
        [Range(0f, 1f)]
        [SerializeField] private float _ventStrength = 0.55f;

        [Tooltip("How much of each patch's radius fades out toward its rim (0..1): high, so the patches blend into " +
            "the dust like a stain, not a blotch.")]
        [Range(0f, 1f)]
        [SerializeField] private float _ventEdgeSoftness = 0.9f;

        [Tooltip("Distance (metres) where the field starts to fade, and where it is gone: readable across the basin.")]
        [SerializeField] private Vector2 _ventFade = new Vector2(160f, 380f);

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

        public float RouteBearing => _routeBearing;
        public float[] RoutePostDistances => _routePostDistances;
        public float RoutePostSide => _routePostSide;
        public float ReflectorGlow => _reflectorGlow;
        public float BridgeLampGlow => _bridgeLampGlow;
        public float BridgeSide => _bridgeSide;
        public float DishSpread => _dishSpread;
        public int VentPatches => _ventPatches;
        public Vector2 VentPatchRadius => _ventPatchRadius;
        public float VentSpread => _ventSpread;
        public Color VentTint => _ventTint;
        public float VentStrength => _ventStrength;
        public float VentEdgeSoftness => _ventEdgeSoftness;
        public Vector2 VentFade => _ventFade;
        [Header("The landed cargo lander (opening v2 §4)")]
        [Tooltip("How far the landed lander leans off level, degrees, about the axis from its port side to the far " +
            "side: a hard landing on uneven ground, slightly tilted.")]
        [Range(0f, 8f)]
        [SerializeField] private float _landerTilt = 2.5f;

        public RuinPlacement[] Ruins => _ruins;
        [Tooltip("07's old charging cable from its dead post at the wake point to the lander's port.")]
        [SerializeField] private GroundCableSettings _wakeCable = new GroundCableSettings();

        public float LanderTilt => _landerTilt;
        public GroundCableSettings WakeCable => _wakeCable;
    }
}
