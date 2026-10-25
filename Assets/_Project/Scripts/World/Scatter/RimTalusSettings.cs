using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The talus at the feet of the crater's walls (Art's rim talus kit, RimTalus_00..07 from small to large): fallen
    /// blocks heaped where the wall's slope breaks onto the floor, the big ones at the foot, smaller ones fanning out
    /// over the first metres of floor and densest under the steepest ground; and small stones only along the canyon's
    /// and the slot's walls, which must stay unclimbable and keep 07's drive line clear.
    /// </summary>
    [Serializable]
    public sealed class RimTalusSettings
    {
        [Header("Where the wall's foot is")]
        [Tooltip("Slope, degrees, where the ground going out from the floor first turns into the wall: the foot the " +
            "talus heaps against.")]
        [Range(10f, 45f)]
        [SerializeField] private float _footSlope = 22f;

        [Tooltip("How far inside the floor's edge the search for the wall's foot starts, metres.")]
        [Range(0f, 60f)]
        [SerializeField] private float _footSearchInside = 20f;

        [Tooltip("How far outside the floor's edge the search for the wall's foot ends, metres.")]
        [Range(20f, 200f)]
        [SerializeField] private float _footSearchOut = 110f;

        [Tooltip("How far up the wall, metres, the slope is read to find where the wall above the foot is steep.")]
        [Range(2f, 30f)]
        [SerializeField] private float _steepProbe = 10f;

        [Tooltip("Slope of the wall above the foot, degrees, from which the talus heaps densest (under steep risers).")]
        [Range(20f, 70f)]
        [SerializeField] private float _steepSlope = 38f;

        [Tooltip("How much denser the small and middle talus heap under steep ground.")]
        [Range(1f, 4f)]
        [SerializeField] private float _steepBoost = 1.8f;

        [Header("Along the rim")]
        [Tooltip("Spacing of the big blocks along the wall's foot, metres: one every so often, never even.")]
        [SerializeField] private Vector2 _anchorSpacing = new Vector2(15f, 25f);

        [Tooltip("How far out from the foot the big blocks lie, metres.")]
        [SerializeField] private Vector2 _anchorBand = new Vector2(0f, 2f);

        [Tooltip("How far out from the foot the middle blocks lie, metres.")]
        [SerializeField] private Vector2 _midBand = new Vector2(2f, 6f);

        [Tooltip("How far out from the foot the small stones fan, metres.")]
        [SerializeField] private Vector2 _smallBand = new Vector2(0f, 8f);

        [Tooltip("Middle blocks round each big one (before the steep boost).")]
        [SerializeField] private Vector2 _midsPerAnchor = new Vector2(1f, 3f);

        [Tooltip("Small stones round each big one (before the steep boost).")]
        [SerializeField] private Vector2 _smallsPerAnchor = new Vector2(2f, 5f);

        [Tooltip("Ground kept clear of talus round the ways out, metres: either side of the rille's pass and round the " +
            "Peak road's foot, whose own rockfall dresses it.")]
        [Range(0f, 120f)]
        [SerializeField] private float _exitClear = 40f;

        [Tooltip("Ground kept clear of talus beyond each world anchor's own clearing, metres.")]
        [Range(0f, 20f)]
        [SerializeField] private float _anchorClear = 2f;

        [Header("The kit")]
        [Tooltip("First kit piece of each group, by index (RimTalus_00..07): small stones from 0, middle blocks from " +
            "this one, big blocks from the next.")]
        [Range(1, 6)]
        [SerializeField] private int _firstMid = 2;

        [Tooltip("First big block in the kit, by index.")]
        [Range(2, 7)]
        [SerializeField] private int _firstBig = 5;

        [Tooltip("Scale of each piece against its own size, picked at random.")]
        [SerializeField] private Vector2 _scale = new Vector2(0.85f, 1.15f);

        [Tooltip("Share of each piece's height sunk into the dust, picked at random.")]
        [SerializeField] private Vector2 _sink = new Vector2(0.1f, 0.25f);

        [Tooltip("Width a piece needs, metres, to be solid (a convex collider on the Prop layer): the big blocks.")]
        [Range(0.5f, 6f)]
        [SerializeField] private float _solidFromSize = 2.2f;

        [Header("The canyon and the slot")]
        [Tooltip("Spacing of the small stones along each side of the canyon's and the slot's floors, metres.")]
        [Range(1f, 20f)]
        [SerializeField] private float _corridorSpacing = 4f;

        [Tooltip("How far from the wall's foot a corridor's stone may lie, metres: tight against it, so nothing " +
            "builds a ramp up the wall or blocks the drive line.")]
        [Range(0.1f, 3f)]
        [SerializeField] private float _corridorReach = 1.5f;

        [Tooltip("Last kit piece, by index, a corridor may hold: only the smallest pieces, lower than 07's step.")]
        [Range(0, 3)]
        [SerializeField] private int _corridorLast = 2;

        public float FootSlope => _footSlope;
        public float FootSearchInside => _footSearchInside;
        public float FootSearchOut => _footSearchOut;
        public float ExitClear => _exitClear;
        public float AnchorClear => _anchorClear;
        public float SteepProbe => _steepProbe;
        public float SteepSlope => _steepSlope;
        public float SteepBoost => _steepBoost;
        public Vector2 AnchorSpacing => _anchorSpacing;
        public Vector2 AnchorBand => _anchorBand;
        public Vector2 MidBand => _midBand;
        public Vector2 SmallBand => _smallBand;
        public Vector2 MidsPerAnchor => _midsPerAnchor;
        public Vector2 SmallsPerAnchor => _smallsPerAnchor;
        public int FirstMid => _firstMid;
        public int FirstBig => _firstBig;
        public Vector2 Scale => _scale;
        public Vector2 Sink => _sink;
        public float SolidFromSize => _solidFromSize;
        public float CorridorSpacing => _corridorSpacing;
        public float CorridorReach => _corridorReach;
        public int CorridorLast => _corridorLast;

        /// <summary>Returns null when the talus is consistent, otherwise the problem.</summary>
        public string Validate()
        {
            if (_firstMid >= _firstBig)
            {
                return "Rim talus: the middle blocks must start before the big ones.";
            }

            if (_anchorSpacing.x <= 0f || _anchorSpacing.y < _anchorSpacing.x)
            {
                return "Rim talus: the big blocks' spacing needs 0 < min <= max.";
            }

            if (_corridorLast >= _firstBig)
            {
                return "Rim talus: the canyon's stones must stay smaller than the big blocks.";
            }

            return null;
        }
    }
}
