using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Where rocks lie: Poisson-disk spacing per class, density by region (clusters, crater rims, the talus at the
    /// foot of the rim), and the places kept clear (the base pad, play features and the driving lanes).
    /// </summary>
    [Serializable]
    public sealed class ScatterSettings
    {
        [Header("Extent")]
        [Tooltip("Rocks are scattered inside this radius around the base, metres.")]
        [Range(50f, 1000f)]
        [SerializeField] private float _extentRadius = 370f;

        [Tooltip("Clear ground kept around the base pad, metres beyond the pad radius.")]
        [Range(0f, 60f)]
        [SerializeField] private float _padClearance = 3.5f;

        [Tooltip("Extra clear ground (metres) kept around every world anchor's radius, beyond the rock's own size: " +
            "anchor spaces (a relay mast's pad, an alcove, the terminus) stay uncluttered.")]
        [Range(0f, 10f)]
        [SerializeField] private float _anchorClearance = 1.5f;

        [Tooltip("Scorch weight (0..1) above which no rock lies: Kestrel-3's crater, its furrow and their dark " +
            "halo stay clear, an open driving line up to the wreck.")]
        [Range(0f, 1f)]
        [SerializeField] private float _scorchClear = 0.05f;

        [Header("Driving lanes")]
        [Tooltip("Half width of the boulder-free lanes from the base to The Peak, the ramps, the bowls and the " +
            "extra bearings below, metres.")]
        [Range(0f, 40f)]
        [SerializeField] private float _laneHalfWidth = 13f;

        [Tooltip("Extra boulder-free lanes from the base to the rim, as bearings in degrees clockwise from +Z.")]
        [SerializeField] private float[] _laneBearings = { 325f, 95f, 165f, 232f, 250f };

        [Tooltip("Inside Whispering Canyon, pebbles only lie this far out from the centre line (0 = centre, 1 = wall " +
            "foot), so the driving line stays clear.")]
        [Range(0f, 1f)]
        [SerializeField] private float _canyonPebbleClear = 0.65f;

        [Tooltip("Pebble density kept inside lanes (fraction).")]
        [Range(0f, 1f)]
        [SerializeField] private float _laneKeptPebbles = 0.5f;

        [Header("Clusters")]
        [Tooltip("Size of the rocky and clean patches, metres.")]
        [Range(10f, 500f)]
        [SerializeField] private float _clusterWavelength = 58f;

        [Tooltip("How strongly rocks gather into clusters (0 = even, 1 = strongly clumped).")]
        [Range(0f, 1f)]
        [SerializeField] private float _clusterContrast = 0.85f;

        [Header("Pebbles & Cobbles (wheel-size collider cutoff)")]
        [Tooltip("Minimum distance between pebbles, metres.")]
        [Range(0.5f, 20f)]
        [SerializeField] private float _pebbleSpacing = 5f;

        [Tooltip("Pebble size range (x = min, y = max), metres.")]
        [SerializeField] private Vector2 _pebbleSize = new Vector2(0.18f, 0.46f);

        [Tooltip("Wheel-size collider cutoff (metres): 07's wheel radius is 0.35 m (wheel diameter 0.70 m, belly-pan " +
            "clearance 0.335 m). Stones with horizontal size >= 0.48 m (~0.69x wheel diameter, exposed height >= 0.30 m " +
            "after ~10% burial) reach the wheel hub / belly pan and get a cheap SphereCollider on Layers.Prop; smaller " +
            "pebbles (< 0.48 m) stay below the axle and remain non-colliding.")]
        [Range(0.25f, 1.0f)]
        [SerializeField] private float _cobbleColliderMinSize = 0.48f;

        [Tooltip("Average fraction of pebble sites that get a pebble.")]
        [Range(0f, 1f)]
        [SerializeField] private float _pebbleDensity = 0.34f;

        [Tooltip("No pebbles on faces steeper than this, degrees.")]
        [Range(0f, 60f)]
        [SerializeField] private float _pebbleMaxSlope = 28f;

        [Tooltip("Pebbles fade out of view beyond about this distance, metres (they are a few pixels there).")]
        [Range(20f, 600f)]
        [SerializeField] private float _pebbleCullDistance = 150f;

        [Header("Boulders (colliders on Layers.Prop)")]
        [Tooltip("Minimum distance between boulders, metres.")]
        [Range(2f, 80f)]
        [SerializeField] private float _boulderSpacing = 4.2f;

        [Tooltip("Boulder size range (x = min, y = max), metres.")]
        [SerializeField] private Vector2 _boulderSize = new Vector2(0.85f, 3.6f);

        [Tooltip("Base fraction of boulder sites that get a boulder on the open floor.")]
        [Range(0f, 1f)]
        [SerializeField] private float _boulderDensity = 0.025f;

        [Header("Rubble")]
        [Tooltip("Grid cell of the near-field rubble, metres: at most one clod or stone per cell (owner's Apollo 11 " +
            "reference: the ground near the camera is full of small rocks and clods).")]
        [Range(0.3f, 3f)]
        [SerializeField] private float _rubbleSpacing = 0.6f;

        [Tooltip("Rubble size range (x = min, y = max), metres across: clods and small stones, all below the cobble " +
            "collider cutoff, so they never collide.")]
        [SerializeField] private Vector2 _rubbleSize = new Vector2(0.08f, 0.3f);

        [Tooltip("Share of rubble cells that hold a stone on the plain's emptier stretches (0..1).")]
        [Range(0f, 1f)]
        [SerializeField] private float _rubbleSparse = 0.06f;

        [Tooltip("Share of rubble cells that hold a stone inside a rubble patch (0..1).")]
        [Range(0f, 1f)]
        [SerializeField] private float _rubbleDense = 0.65f;

        [Tooltip("Size of the rubble patches and the emptier stretches between them, metres.")]
        [Range(5f, 200f)]
        [SerializeField] private float _rubblePatchWavelength = 24f;

        [Tooltip("Share of the plain the rubble patches cover (0..1).")]
        [Range(0f, 1f)]
        [SerializeField] private float _rubblePatchCoverage = 0.35f;

        [Tooltip("Stones in the apron round each boulder, per metre of the boulder's size.")]
        [Range(0f, 80f)]
        [SerializeField] private float _rubbleApronCount = 26f;

        [Tooltip("How far the apron reaches from a boulder's middle, in the boulder's sizes.")]
        [Range(0.6f, 6f)]
        [SerializeField] private float _rubbleApronReach = 2.4f;

        [Tooltip("Rubble keeps this far from the centre line of every driving lane and protected lane, metres: " +
            "narrower than the pebbles' lanes, so the rubble lies close beside the opening's drives and reads from " +
            "the chase camera, while the line 07 drives stays clean.")]
        [Range(2f, 20f)]
        [SerializeField] private float _rubbleLaneHalfWidth = 6f;

        [Tooltip("Rubble shows only within about this distance of the camera, metres: it reads from the chase " +
            "camera, and stays cheap.")]
        [Range(10f, 120f)]
        [SerializeField] private float _rubbleCullDistance = 40f;

        [Tooltip("Rubble is meshed in chunks this wide, metres: small, so the distance cull follows the camera " +
            "closely.")]
        [Range(4f, 64f)]
        [SerializeField] private float _rubbleChunkSize = 32f;

        [Tooltip("Size of the boulder fields and the long empty stretches between them, metres: boulders gather in a " +
            "few places and leave open plain elsewhere.")]
        [Range(80f, 800f)]
        [SerializeField] private float _boulderFieldWavelength = 320f;

        [Tooltip("Share of the plain the boulder fields cover (0..1): the rest is left bare of boulders.")]
        [Range(0f, 1f)]
        [SerializeField] private float _boulderFieldCoverage = 0.35f;

        [Tooltip("How much denser boulders lie inside a field than the open floor's base chance (times).")]
        [Range(1f, 10f)]
        [SerializeField] private float _boulderFieldDensity = 3f;

        [Tooltip("Extra boulder chance on raised crater rims (ejecta).")]
        [Range(0f, 1f)]
        [SerializeField] private float _craterRimBoulders = 0.82f;

        [Tooltip("Extra boulder chance in the talus band at the foot of the rim: a boulder apron, so the wall " +
            "stands on its own fallen rock.")]
        [Range(0f, 1f)]
        [SerializeField] private float _talusBoulders = 0.55f;

        [Tooltip("Centre of the talus band: metres outside the floor edge (negative = on the floor).")]
        [Range(-60f, 60f)]
        [SerializeField] private float _talusOffset = 8f;

        [Tooltip("Half width of the talus band, metres.")]
        [Range(1f, 80f)]
        [SerializeField] private float _talusHalfWidth = 26f;

        [Tooltip("No boulders on faces steeper than this, degrees.")]
        [Range(0f, 60f)]
        [SerializeField] private float _boulderMaxSlope = 24f;

        [Tooltip("Clear ground kept around ramps and play bowls, metres.")]
        [Range(0f, 40f)]
        [SerializeField] private float _featureClearance = 8f;

        [Header("Big boulders (Art's R1 Rock_10..13)")]
        [Tooltip("The rare big boulders the scene build stands on the basin floor, off the opening's routes.")]
        [SerializeField] private BigBoulderSettings _bigBoulders = new BigBoulderSettings();

        public BigBoulderSettings BigBoulders => _bigBoulders;
        public float ExtentRadius => _extentRadius;
        public float PadClearance => _padClearance;
        public float AnchorClearance => _anchorClearance;
        public float ScorchClear => _scorchClear;
        public float LaneHalfWidth => _laneHalfWidth;
        public float[] LaneBearings => _laneBearings;
        public float LaneKeptPebbles => _laneKeptPebbles;
        public float CanyonPebbleClear => _canyonPebbleClear;
        public float ClusterWavelength => _clusterWavelength;
        public float ClusterContrast => _clusterContrast;
        public float PebbleSpacing => _pebbleSpacing;
        public Vector2 PebbleSize => _pebbleSize;
        public float CobbleColliderMinSize => _cobbleColliderMinSize;
        public float PebbleDensity => _pebbleDensity;
        public float PebbleMaxSlope => _pebbleMaxSlope;
        public float PebbleCullDistance => _pebbleCullDistance;
        public float BoulderSpacing => _boulderSpacing;
        public Vector2 BoulderSize => _boulderSize;
        public float BoulderDensity => _boulderDensity;
        public float RubbleSpacing => _rubbleSpacing;
        public Vector2 RubbleSize => _rubbleSize;
        public float RubbleSparse => _rubbleSparse;
        public float RubbleDense => _rubbleDense;
        public float RubblePatchWavelength => _rubblePatchWavelength;
        public float RubblePatchCoverage => _rubblePatchCoverage;
        public float RubbleApronCount => _rubbleApronCount;
        public float RubbleApronReach => _rubbleApronReach;
        public float RubbleLaneHalfWidth => _rubbleLaneHalfWidth;
        public float RubbleCullDistance => _rubbleCullDistance;
        public float RubbleChunkSize => _rubbleChunkSize;
        public float BoulderFieldWavelength => _boulderFieldWavelength;
        public float BoulderFieldCoverage => _boulderFieldCoverage;
        public float BoulderFieldDensity => _boulderFieldDensity;
        public float CraterRimBoulders => _craterRimBoulders;
        public float TalusBoulders => _talusBoulders;
        public float TalusOffset => _talusOffset;
        public float TalusHalfWidth => _talusHalfWidth;
        public float BoulderMaxSlope => _boulderMaxSlope;
        public float FeatureClearance => _featureClearance;

        /// <summary>Returns null when the settings are consistent, otherwise a description of the problem.</summary>
        public string Validate()
        {
            if (_pebbleSize.x <= 0f || _pebbleSize.x > _pebbleSize.y || _boulderSize.x <= 0f
                || _boulderSize.x > _boulderSize.y)
            {
                return "Rock size ranges need 0 < min <= max.";
            }

            if (_cobbleColliderMinSize <= 0f || _cobbleColliderMinSize > _boulderSize.x)
            {
                return "Cobble collider cutoff must be positive and no larger than the minimum boulder size.";
            }

            if (_boulderSize.y > _boulderSpacing)
            {
                return "Boulders must be smaller than their spacing or they would overlap.";
            }

            return null;
        }
    }
}
