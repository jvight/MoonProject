using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The lander's blast-swept zone (opening v2 §4): its descent engine smoothed the regolith round it as it touched
    /// down, so the ground there reflects a little more, like the soft halos round the Apollo sites seen from orbit:
    /// a diffuse brightening toward the lander with a blotchy edge, crossed by a few faint, short, uneven rays; bare
    /// of rocks inside and piled thicker with them at its edge. Rovers park outside it, which is why 07's old post
    /// stands out on the plain.
    /// </summary>
    [Serializable]
    public sealed class BlastZoneSettings
    {
        [Tooltip("Radius of the swept zone round the lander's centre, metres (its edge wanders either side of it).")]
        [Range(10f, 60f)]
        [SerializeField] private float _radius = 34f;

        [Tooltip("Metres inside the radius over which the rocks come back (the scatter's edge, not the halo's).")]
        [Range(1f, 20f)]
        [SerializeField] private float _edgeBand = 9f;

        [Header("The halo")]
        [Tooltip("How much lighter the regolith is at the lander, easing to nothing at the edge (0 = no change): " +
            "low contrast, smoothed ground reflecting a little more, never a painted disc.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _haloLift = 0.38f;

        [Tooltip("How the halo falls off from the lander to its edge (a power of the share left): 1 is a straight " +
            "ramp, higher keeps the brightening close to the lander.")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _haloFalloff = 1.1f;

        [Tooltip("Metres the halo's edge wanders in and out of the radius, so it is never a circle.")]
        [Range(0f, 15f)]
        [SerializeField] private float _edgeWander = 7f;

        [Tooltip("Size of the blotches that break the halo's edge and mottle it, metres.")]
        [Range(2f, 40f)]
        [SerializeField] private float _blotchSize = 9f;

        [Tooltip("How much the blotches mottle the halo inside (0 = an even halo).")]
        [Range(0f, 1f)]
        [SerializeField] private float _blotchContrast = 0.4f;

        [Header("The rays (secondary)")]
        [Tooltip("How much lighter a ray is than the halo under it at its brightest: faint, a third of a spoke.")]
        [Range(0f, 0.3f)]
        [SerializeField] private float _rayLift = 0.08f;

        [Tooltip("Sectors round the lander that may each hold one ray at a random bearing (whole number).")]
        [Range(8, 160)]
        [SerializeField] private int _raySectors = 64;

        [Tooltip("Share of the sectors that hold a ray, so the spacing is uneven.")]
        [Range(0f, 1f)]
        [SerializeField] private float _rayChance = 0.4f;

        [Tooltip("Length of the shortest rays, metres: short streaks, never spokes to the edge.")]
        [Range(1f, 10f)]
        [SerializeField] private float _rayLengthMin = 3f;

        [Tooltip("Length of the longest of the usual rays, metres.")]
        [Range(2f, 20f)]
        [SerializeField] private float _rayLengthMax = 8f;

        [Tooltip("Share of the rays that run long.")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _longRayChance = 0.12f;

        [Tooltip("Length of a long ray, metres (each varies by a quarter either way).")]
        [Range(5f, 30f)]
        [SerializeField] private float _longRayLength = 14f;

        [Tooltip("Width of the narrowest rays at their widest, metres; each tapers to nothing at both ends.")]
        [Range(0.1f, 2f)]
        [SerializeField] private float _rayWidthMin = 0.4f;

        [Tooltip("Width of the widest rays at their widest, metres.")]
        [Range(0.2f, 4f)]
        [SerializeField] private float _rayWidthMax = 1.4f;

        [Header("The ground")]
        [Tooltip("The sweep shows only on ground at least this level (its normal's up component): the plume skimmed " +
            "the flat, it never climbed slopes or hills.")]
        [Range(0.8f, 1f)]
        [SerializeField] private float _levelGround = 0.95f;

        [Header("The rocks")]
        [Tooltip("Extra chance of a pebble on the zone's edge, where the blast piled what it swept (0 = none).")]
        [Range(0f, 2f)]
        [SerializeField] private float _edgePebbles = 0.55f;

        [Tooltip("Width of the pebble pile round the edge, metres, centred just outside the radius.")]
        [Range(1f, 20f)]
        [SerializeField] private float _edgePileWidth = 8f;

        public float Radius => _radius;
        public float EdgeBand => _edgeBand;
        public float HaloLift => _haloLift;
        public float HaloFalloff => _haloFalloff;
        public float EdgeWander => _edgeWander;
        public float BlotchSize => _blotchSize;
        public float BlotchContrast => _blotchContrast;
        public float RayLift => _rayLift;
        public int RaySectors => _raySectors;
        public float RayChance => _rayChance;
        public float RayLengthMin => _rayLengthMin;
        public float RayLengthMax => _rayLengthMax;
        public float LongRayChance => _longRayChance;
        public float LongRayLength => _longRayLength;
        public float RayWidthMin => _rayWidthMin;
        public float RayWidthMax => _rayWidthMax;
        public float LevelGround => _levelGround;
        public float EdgePebbles => _edgePebbles;
        public float EdgePileWidth => _edgePileWidth;
    }
}
