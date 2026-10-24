using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// The rille notch (base-home-and-crater-exits §4, the 0.6 west exit): a rock-walled pass cut through the rim just
    /// past relay.1, its floor low through the wall and rising beyond the crest onto the dark mare, so from relay.1
    /// the notch's walls stand against the sky with a band of black plain between them. Until the mare is built (0.6) a
    /// steep lava lip just past the crest ends the drive out: 07 can drive into the pass and look, never off the world.
    /// </summary>
    [Serializable]
    public sealed class RilleSettings
    {
        [Tooltip("Bearing of the pass from the base, degrees clockwise from +Z: just past relay.1 (about 250 deg), " +
            "so it lies in plain view from the relit relay.")]
        [Range(0f, 360f)]
        [SerializeField] private float _bearing = 255f;

        [Tooltip("Distance from the base where the pass begins to cut into the rim's foothills, metres.")]
        [Range(200f, 400f)]
        [SerializeField] private float _start = 300f;

        [Tooltip("Run over which the cut fades in from the foothills, metres: the floor meets the pass gently.")]
        [Range(5f, 100f)]
        [SerializeField] private float _startBlend = 40f;

        [Tooltip("Height of the pass's floor through the wall, metres above the base pad: a little above the basin's " +
            "edge, a low gap in the rim.")]
        [Range(0f, 30f)]
        [SerializeField] private float _passHeight = 7f;

        [Tooltip("Distance past the crest, metres, of the lava lip that ends the drive out of the pass (0.6 opens it).")]
        [Range(0f, 80f)]
        [SerializeField] private float _lipDistance = 25f;

        [Tooltip("Height of the lava lip, metres: low enough that the dark mare shows over it from relay.1, too steep " +
            "to climb.")]
        [Range(2f, 20f)]
        [SerializeField] private float _lipHeight = 6f;

        [Tooltip("Horizontal run of the lava lip, metres: short, so it stands far steeper than 07 can climb.")]
        [Range(1f, 10f)]
        [SerializeField] private float _lipRun = 5f;

        [Tooltip("Distance past the crest, metres, where the pass's floor starts to rise onto the mare.")]
        [Range(0f, 200f)]
        [SerializeField] private float _riseStart = 20f;

        [Tooltip("Distance past the crest, metres, where the pass's floor has risen to the outer plain: the rising " +
            "dark ground fills the bottom of the notch seen from relay.1.")]
        [Range(20f, 400f)]
        [SerializeField] private float _riseEnd = 140f;

        [Tooltip("Distance from the base where the cut ends on the outer plain, metres.")]
        [Range(400f, 1200f)]
        [SerializeField] private float _end = 680f;

        [Tooltip("Run over which the cut fades out at its far end, metres.")]
        [Range(5f, 200f)]
        [SerializeField] private float _endBlend = 60f;

        [Tooltip("Half width of the pass's flat floor, metres.")]
        [Range(4f, 60f)]
        [SerializeField] private float _halfWidth = 14f;

        [Tooltip("Slope of the notch's walls, degrees: steep enough to read as rock against the sky.")]
        [Range(20f, 75f)]
        [SerializeField] private float _wallSlope = 55f;

        [Tooltip("Soft run where the floor turns up into the walls, metres.")]
        [Range(0.5f, 20f)]
        [SerializeField] private float _wallEase = 5f;

        [Tooltip("Rounding where the cut meets the natural rim, metres: no crease along the notch's top edges.")]
        [Range(0.5f, 20f)]
        [SerializeField] private float _rounding = 4f;

        [Header("The dark mare beyond")]
        [Tooltip("Half angle of the dark mare's sector round the pass's bearing, degrees: the black plain the notch " +
            "looks onto.")]
        [Range(2f, 90f)]
        [SerializeField] private float _mareHalfAngle = 28f;

        [Tooltip("Share of the half angle over which the mare's edge fades to the highland's grey.")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _mareEdge = 0.4f;

        [Tooltip("Distance from the crest, metres (negative inside it), where the mare's dark ground begins.")]
        [Range(-100f, 100f)]
        [SerializeField] private float _mareStart = -30f;

        [Tooltip("Run over which the mare darkens in, metres.")]
        [Range(5f, 200f)]
        [SerializeField] private float _mareBlend = 40f;

        public float Bearing => _bearing;
        public float Start => _start;
        public float StartBlend => _startBlend;
        public float PassHeight => _passHeight;
        public float LipDistance => _lipDistance;
        public float LipHeight => _lipHeight;
        public float LipRun => _lipRun;
        public float RiseStart => _riseStart;
        public float RiseEnd => _riseEnd;
        public float End => _end;
        public float EndBlend => _endBlend;
        public float HalfWidth => _halfWidth;
        public float WallSlope => _wallSlope;
        public float WallEase => _wallEase;
        public float Rounding => _rounding;
        public float MareHalfAngle => _mareHalfAngle;
        public float MareEdge => _mareEdge;
        public float MareStart => _mareStart;
        public float MareBlend => _mareBlend;

        /// <summary>Returns null when the pass is well formed, otherwise the problem.</summary>
        public string Validate(float floorRadius, float crestRadius)
        {
            if (_start >= floorRadius + _startBlend)
            {
                return "the rille must start cutting no later than the floor's edge";
            }

            if (_lipDistance + _lipRun >= _riseEnd)
            {
                return "the rille's lava lip must stand before the rise onto the mare ends";
            }

            if (_riseStart >= _riseEnd)
            {
                return "the rille's rise must end after it starts";
            }

            if (crestRadius + _riseEnd >= _end)
            {
                return "the rille must reach the outer plain before it ends";
            }

            return null;
        }
    }
}
