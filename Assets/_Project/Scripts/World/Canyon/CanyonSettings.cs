using System;
using UnityEngine;

namespace MoonProject.World
{
    /// <summary>
    /// Whispering Canyon (docs/features/M3-04): a narrow canyon cut through the rim, gated by a chasm only a
    /// Hover-Jump crosses, with a one-way step-down exit (design ruling 10). Distances are metres along the canyon's
    /// centre line from its mouth on the basin floor; heights are metres above the floor at the mouth.
    /// </summary>
    [Serializable]
    public sealed class CanyonSettings
    {
        /// <summary>Ease (metres) at both ends of the trough's near side.</summary>
        public const float TroughSideEase = 3f;

        [Header("Placement")]
        [Tooltip("Bearing of the canyon from the base, degrees clockwise from +Z. Read from the base pad: a dark " +
            "notch in the lit eastern rim, clear of The Peak, Earth and the spawn view.")]
        [Range(0f, 360f)]
        [SerializeField] private float _bearing = 72f;

        [Tooltip("Distance of the canyon mouth from the base, metres. Keep the whole canyon outside PlayableArea.")]
        [Range(150f, 400f)]
        [SerializeField] private float _mouthDistance = 280f;

        [Header("Take-off and chasm")]
        [Tooltip("Flat approach from the mouth to the foot of the take-off ramp.")]
        [Range(0f, 40f)]
        [SerializeField] private float _approachLength = 6f;

        [Tooltip("Length of the take-off ramp up to the lip.")]
        [Range(4f, 40f)]
        [SerializeField] private float _rampLength = 12f;

        [Tooltip("Height of the lip above the approach.")]
        [Range(0f, 5f)]
        [SerializeField] private float _rampHeight = 1.6f;

        [Tooltip("Distance from the lip to the far face's centre; its rock slab starts 3.6 m before it, so 22.1 " +
            "leaves an 18.5 m gap. Normal hops (~1 m apex) never cross; a 3/4 Hover-Jump at cruising speed carries " +
            "about 22 m.")]
        [Range(10f, 35f)]
        [SerializeField] private float _gap = 22.1f;

        [Tooltip("Depth of the chasm trough below the lip. It reads deep through darkness, not geometry.")]
        [Range(1f, 10f)]
        [SerializeField] private float _troughDepth = 4.5f;

        [Tooltip("Steepest slope (degrees) of the trough's near side: always drivable back out.")]
        [Range(5f, 20f)]
        [SerializeField] private float _troughSideSlope = 18f;

        [Tooltip("Height of the landing apron above the lip: higher than any normal hop can reach.")]
        [Range(0f, 5f)]
        [SerializeField] private float _apronRaise = 2f;

        [Tooltip("Length of the flat landing apron beyond the far face (room for a misjudged leap).")]
        [Range(10f, 80f)]
        [SerializeField] private float _apronLength = 38f;

        [Tooltip("Horizontal run of the far face and of the exit step in the height function (the rock slab built on " +
            "them makes them vertical).")]
        [Range(0.2f, 3f)]
        [SerializeField] private float _faceRun = 0.6f;

        [Header("Canyon")]
        [Tooltip("Half width of the open mouth, metres.")]
        [Range(5f, 40f)]
        [SerializeField] private float _mouthHalfWidth = 20f;

        [Tooltip("Half width over the chasm and the apron: a generous corridor to aim a leap along.")]
        [Range(5f, 30f)]
        [SerializeField] private float _chasmHalfWidth = 12f;

        [Tooltip("Half width of the winding canyon (the spec asks for 12-25 m wide).")]
        [Range(4f, 15f)]
        [SerializeField] private float _canyonHalfWidth = 8f;

        [Tooltip("Distance from the mouth where the canyon starts bending along the rim.")]
        [Range(80f, 300f)]
        [SerializeField] private float _bendStart = 120f;

        [Tooltip("Length of the bend that turns the canyon to run along the rim.")]
        [Range(20f, 200f)]
        [SerializeField] private float _bendLength = 80f;

        [Tooltip("Total turn of the bend, degrees (positive = clockwise seen from above).")]
        [Range(-150f, 150f)]
        [SerializeField] private float _bendAngle = 95f;

        [Tooltip("Total length of the canyon from the mouth to the terminus.")]
        [Range(150f, 500f)]
        [SerializeField] private float _length = 330f;

        [Tooltip("Amplitude of the canyon's winding after the bend, degrees of heading.")]
        [Range(0f, 40f)]
        [SerializeField] private float _windAmplitude = 16f;

        [Tooltip("Wavelength of the winding, metres.")]
        [Range(40f, 300f)]
        [SerializeField] private float _windWavelength = 120f;

        [Tooltip("Floor height of the terminus above the landing apron: the canyon climbs gently toward it.")]
        [Range(0f, 60f)]
        [SerializeField] private float _climb = 24f;

        [Tooltip("Radius of the round terminus chamber.")]
        [Range(8f, 40f)]
        [SerializeField] private float _terminusRadius = 20f;

        [Header("Walls")]
        [Tooltip("Height of the canyon walls above its floor (they also rise where the rim is low).")]
        [Range(6f, 60f)]
        [SerializeField] private float _wallHeight = 26f;

        [Tooltip("Horizontal run of the walls: short = sheer rock.")]
        [Range(2f, 30f)]
        [SerializeField] private float _wallRun = 7f;

        [Tooltip("Run of the outer flank where the walls stand above lower ground.")]
        [Range(4f, 40f)]
        [SerializeField] private float _flankRun = 10f;

        [Tooltip("Height of the jagged ridges along the wall tops.")]
        [Range(0f, 20f)]
        [SerializeField] private float _wallRoughness = 7f;

        [Header("Alcoves and the glinting ledge")]
        [Tooltip("Number of side alcoves along the canyon, alternating sides.")]
        [Range(0, 6)]
        [SerializeField] private int _alcoveCount = 3;

        [Tooltip("How far an alcove reaches into the wall.")]
        [Range(2f, 20f)]
        [SerializeField] private float _alcoveDepth = 8f;

        [Tooltip("Length of an alcove along the canyon.")]
        [Range(6f, 40f)]
        [SerializeField] private float _alcoveLength = 20f;

        [Tooltip("Height of the ledge above the canyon floor (a gentle mesa: drivable all round).")]
        [Range(0.5f, 4f)]
        [SerializeField] private float _ledgeHeight = 1.4f;

        [Tooltip("Radius of the ledge's flat top.")]
        [Range(2f, 10f)]
        [SerializeField] private float _ledgeRadius = 4.5f;

        [Header("The relay ledge above the terminus chamber (M3-06)")]
        [Tooltip("Arc length along the canyon of the relay mast's ledge, at the mouth of the terminus chamber: close " +
            "enough to the canyon mouth for the relay chain to link.")]
        [Range(150f, 400f)]
        [SerializeField] private float _relayLedgeArc = 310f;

        [Tooltip("Offset of the relay ledge across the corridor, metres (positive = right of the way in).")]
        [Range(-20f, 20f)]
        [SerializeField] private float _relayLedgeLateral = 10f;

        [Tooltip("Height of the relay ledge above the chamber floor: a gentle mesa, drivable all round.")]
        [Range(0.5f, 6f)]
        [SerializeField] private float _relayLedgeHeight = 3f;

        [Tooltip("Radius of the relay ledge's flat top (the mast's pad needs 3 m).")]
        [Range(3f, 10f)]
        [SerializeField] private float _relayLedgeRadius = 4.5f;

        [Header("The faint warm light inside")]
        [Tooltip("Height of the warm light above the glinting ledge, metres.")]
        [Range(0.5f, 15f)]
        [SerializeField] private float _glowHeight = 3.5f;

        [Tooltip("Brightness of the warm light's soft halo.")]
        [Range(0f, 4f)]
        [SerializeField] private float _glowIntensity = 0.9f;

        [Tooltip("Radius of the halo up close, metres.")]
        [Range(0.2f, 6f)]
        [SerializeField] private float _glowRadius = 1.2f;

        [Tooltip("Smallest apparent radius of the halo, degrees, so it still reads from the base.")]
        [Range(0f, 2f)]
        [SerializeField] private float _glowMinAngle = 0.3f;

        [Tooltip("Range of the warm light that pools on the ledge, metres.")]
        [Range(2f, 40f)]
        [SerializeField] private float _glowLightRange = 14f;

        [Tooltip("Intensity of that warm light.")]
        [Range(0f, 10f)]
        [SerializeField] private float _glowLightIntensity = 1.6f;

        [Header("One-way exit (design ruling 10)")]
        [Tooltip("How far to the left of the canyon the exit runs back to the basin, metres: far enough that solid " +
            "rock always separates it from the chasm.")]
        [Range(20f, 80f)]
        [SerializeField] private float _exitOffset = 40f;

        [Tooltip("Radius of the exit's turn onto the landing apron. Keep it wider than the exit's walls.")]
        [Range(10f, 60f)]
        [SerializeField] private float _exitTurnRadius = 28f;

        [Tooltip("Distance along the canyon where the exit joins the landing apron.")]
        [Range(0f, 80f)]
        [SerializeField] private float _exitBranch = 22f;

        [Tooltip("Half width of the exit corridor.")]
        [Range(3f, 12f)]
        [SerializeField] private float _exitHalfWidth = 6f;

        [Tooltip("Height of the exit's step down to the basin: one-way, never climbable, landing gentler than the " +
            "play ramps.")]
        [Range(1.5f, 2.5f)]
        [SerializeField] private float _exitStepHeight = 2.5f;

        [Tooltip("Flat ground at the step's foot before the exit opens into the basin.")]
        [Range(4f, 30f)]
        [SerializeField] private float _exitFootLength = 9f;

        public float Bearing => _bearing;
        public float MouthDistance => _mouthDistance;
        public float ApproachLength => _approachLength;
        public float RampLength => _rampLength;
        public float RampHeight => _rampHeight;
        public float Gap => _gap;
        public float TroughDepth => _troughDepth;
        public float TroughSideSlope => _troughSideSlope;
        public float ApronRaise => _apronRaise;
        public float ApronLength => _apronLength;
        public float FaceRun => _faceRun;
        public float MouthHalfWidth => _mouthHalfWidth;
        public float ChasmHalfWidth => _chasmHalfWidth;
        public float CanyonHalfWidth => _canyonHalfWidth;
        public float BendStart => _bendStart;
        public float BendLength => _bendLength;
        public float BendAngle => _bendAngle;
        public float Length => _length;
        public float WindAmplitude => _windAmplitude;
        public float WindWavelength => _windWavelength;
        public float Climb => _climb;
        public float TerminusRadius => _terminusRadius;
        public float WallHeight => _wallHeight;
        public float WallRun => _wallRun;
        public float FlankRun => _flankRun;
        public float WallRoughness => _wallRoughness;
        public int AlcoveCount => _alcoveCount;
        public float AlcoveDepth => _alcoveDepth;
        public float AlcoveLength => _alcoveLength;
        public float LedgeHeight => _ledgeHeight;
        public float RelayLedgeArc => _relayLedgeArc;
        public float RelayLedgeLateral => _relayLedgeLateral;
        public float RelayLedgeHeight => _relayLedgeHeight;
        public float RelayLedgeRadius => _relayLedgeRadius;
        public float LedgeRadius => _ledgeRadius;
        public float GlowHeight => _glowHeight;
        public float GlowIntensity => _glowIntensity;
        public float GlowRadius => _glowRadius;
        public float GlowMinAngle => _glowMinAngle;
        public float GlowLightRange => _glowLightRange;
        public float GlowLightIntensity => _glowLightIntensity;
        public float ExitOffset => _exitOffset;
        public float ExitTurnRadius => _exitTurnRadius;
        public float ExitBranch => _exitBranch;
        public float ExitHalfWidth => _exitHalfWidth;
        public float ExitStepHeight => _exitStepHeight;
        public float ExitFootLength => _exitFootLength;

        /// <summary>Returns null when the canyon is consistent, otherwise the problem.</summary>
        public string Validate()
        {
            if (_relayLedgeArc <= _bendStart + _bendLength || _relayLedgeArc > _length)
            {
                return "Canyon: the relay ledge must lie past the bend, inside the canyon.";
            }

            float apronEnd = _approachLength + _rampLength + _gap + _apronLength;
            if (_bendStart <= apronEnd || _bendStart + _bendLength >= _length - _terminusRadius)
            {
                return "Canyon: the bend must start after the landing apron and end before the terminus.";
            }

            if (_exitBranch >= _apronLength || _exitBranch < Canyon.SlabHalfDepth + _exitHalfWidth + _wallRun + 4f)
            {
                return "Canyon: the exit must join the landing apron well past the chasm's far face.";
            }

            if (_exitOffset < _chasmHalfWidth + _exitHalfWidth + 2f * _wallRun + 4f
                || _exitOffset < _exitTurnRadius + _chasmHalfWidth - _exitHalfWidth)
            {
                return "Canyon: the exit runs too close to the chasm, or its turn does not fit; widen its offset.";
            }

            float side = _troughDepth / Mathf.Tan(_troughSideSlope * Mathf.Deg2Rad) + TroughSideEase;
            if (side + Canyon.SlabHalfDepth + 1f > _gap)
            {
                return "Canyon: the trough's gentle near side does not fit in the gap; deepen less or widen the gap.";
            }

            return null;
        }
    }
}
