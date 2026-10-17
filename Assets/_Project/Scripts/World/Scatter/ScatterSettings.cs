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
