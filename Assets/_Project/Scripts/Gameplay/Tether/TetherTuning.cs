using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The energy tether: generous aiming, the winch, the PD spring that floats a relic along behind 07, the soft
    /// anti-frustration release, and the beam's look. Created by the Gameplay/Tuning builder; runtime code only reads
    /// it.
    /// </summary>
    public sealed class TetherTuning : ScriptableObject
    {
        [Header("Aim (generous)")]
        [Tooltip("A relic within this angle (degrees) of the screen centre can be picked: no pixel hunting.")]
        [Range(1f, 30f)] [SerializeField] private float _aimCone = 8f;

        [Tooltip("The hovered relic stays picked until it leaves this wider angle (sticky hover).")]
        [Range(1f, 45f)] [SerializeField] private float _stickyCone = 12f;

        [Tooltip("Farthest a relic can be picked from the camera (m).")]
        [Range(5f, 100f)] [SerializeField] private float _aimRange = 32f;

        [Tooltip("Degrees added per metre of distance when choosing between relics in the cone (nearer wins ties).")]
        [Range(0f, 1f)] [SerializeField] private float _distanceWeight = 0.08f;

        [Tooltip("Halo brightness of the hovered relic, before pressing.")]
        [Range(0f, 4f)] [SerializeField] private float _hoverHighlight = 1f;

        [Tooltip("Halo brightness of the towed relic.")]
        [Range(0f, 4f)] [SerializeField] private float _towHighlight = 0.6f;

        [Header("Winch")]
        [Tooltip("Shortest tether (m) from 07's eye.")]
        [Range(1f, 10f)] [SerializeField] private float _minLength = 2.5f;

        [Tooltip("Longest tether (m).")]
        [Range(2f, 30f)] [SerializeField] private float _maxLength = 12f;

        [Tooltip("Metres the tether changes per scroll notch.")]
        [Range(0.1f, 5f)] [SerializeField] private float _winchStep = 0.9f;

        [Tooltip("Fastest the tether reels (m/s): the length always eases, never jumps.")]
        [Range(0.5f, 20f)] [SerializeField] private float _reelSpeed = 4f;

        [Tooltip("How far (m) the requested length may run ahead of the actual one (a held button never overshoots).")]
        [Range(0.1f, 5f)] [SerializeField] private float _winchLead = 1.5f;

        [Header("Spring")]
        [Tooltip("Natural frequency (rad/s) of the pull for a relic of the reference mass.")]
        [Range(0.5f, 10f)] [SerializeField] private float _naturalFrequency = 3.2f;

        [Tooltip("Mass (kg) the natural frequency is tuned for.")]
        [Range(1f, 40f)] [SerializeField] private float _referenceMass = 6f;

        [Tooltip("How much heavier relics slow the pull (frequency scales with (reference / mass) ^ this).")]
        [Range(0f, 1f)] [SerializeField] private float _massExponent = 0.35f;

        [Tooltip("Damping ratio: above 1 settles without any bounce on the tether itself.")]
        [Range(0.3f, 3f)] [SerializeField] private float _dampingRatio = 1.15f;

        [Tooltip("Largest acceleration (m/s2) the tether gives a relic.")]
        [Range(1f, 50f)] [SerializeField] private float _maxAcceleration = 12f;

        [Tooltip("Largest force (N) the tether can pull with: heavy relics lag and strain.")]
        [Range(10f, 2000f)] [SerializeField] private float _maxForce = 160f;

        [Tooltip("Upward force is capped to this many times the relic's weight: lifting is gentle.")]
        [Range(1f, 5f)] [SerializeField] private float _liftMultiplier = 2.2f;

        [Tooltip("The relic floats this high (m) above the ground while towed.")]
        [Range(0f, 3f)] [SerializeField] private float _hoverHeight = 0.8f;

        [Tooltip("Highest (m) above 07 the tether can lift a relic.")]
        [Range(0.5f, 10f)] [SerializeField] private float _maxLiftHeight = 4f;

        [Tooltip("Linear damping of a towed relic (it trails smoothly).")]
        [Range(0f, 3f)] [SerializeField] private float _towLinearDamping = 0.2f;

        [Tooltip("Angular damping of a towed relic (it turns lazily, never spins).")]
        [Range(0f, 10f)] [SerializeField] private float _towAngularDamping = 1.5f;

        [Header("Soft release")]
        [Tooltip("If the relic hangs this many metres beyond the tether length (stuck)...")]
        [Range(0.5f, 20f)] [SerializeField] private float _snapStretch = 5f;

        [Tooltip("...for this many seconds, the tether lets go softly.")]
        [Range(0f, 5f)] [SerializeField] private float _snapGrace = 0.8f;

        [Tooltip("Beyond this distance (m) from 07's eye the tether lets go at once.")]
        [Range(5f, 60f)] [SerializeField] private float _snapDistance = 24f;

        [Tooltip("Share of its velocity a relic keeps when the tether lets go softly: it stays where it was.")]
        [Range(0f, 1f)] [SerializeField] private float _snapCarry = 0.15f;

        [Header("Beam")]
        [Tooltip("Points along the beam curve.")]
        [Range(4, 64)] [SerializeField] private int _beamPoints = 24;

        [Tooltip("Upward bow of the beam, as a fraction of its length.")]
        [Range(0f, 0.5f)] [SerializeField] private float _beamArc = 0.12f;

        [Tooltip("Gentle energetic wobble (m) of the beam's middle.")]
        [Range(0f, 1f)] [SerializeField] private float _wobble = 0.1f;

        [Tooltip("Wobble frequency (Hz).")]
        [Range(0.1f, 5f)] [SerializeField] private float _wobbleFrequency = 1.3f;

        [Tooltip("Extra wobble (m) at full strain.")]
        [Range(0f, 1f)] [SerializeField] private float _strainWobble = 0.25f;

        [Tooltip("Beam width (m) at 07's eye and at the relic.")]
        [SerializeField] private Vector2 _beamWidth = new Vector2(0.09f, 0.05f);

        [Tooltip("Seconds the beam takes to reach the relic when it latches.")]
        [Range(0.02f, 1f)] [SerializeField] private float _beamExtend = 0.18f;

        [Tooltip("Seconds the beam takes to draw back into the eye when it lets go.")]
        [Range(0.02f, 1f)] [SerializeField] private float _beamRetract = 0.28f;

        [Tooltip("Beam brightness. At or below ~1 it stays cyan instead of blooming white.")]
        [Range(0f, 4f)] [SerializeField] private float _beamIntensity = 1.1f;

        public float AimCone => _aimCone;
        public float StickyCone => Mathf.Max(_stickyCone, _aimCone);
        public float AimRange => _aimRange;
        public float DistanceWeight => _distanceWeight;
        public float HoverHighlight => _hoverHighlight;
        public float TowHighlight => _towHighlight;
        public float MinLength => _minLength;
        public float MaxLength => Mathf.Max(_maxLength, _minLength);
        public float WinchStep => _winchStep;
        public float ReelSpeed => _reelSpeed;
        public float WinchLead => _winchLead;
        public float NaturalFrequency => _naturalFrequency;
        public float ReferenceMass => _referenceMass;
        public float MassExponent => _massExponent;
        public float DampingRatio => _dampingRatio;
        public float MaxAcceleration => _maxAcceleration;
        public float MaxForce => _maxForce;
        public float LiftMultiplier => _liftMultiplier;
        public float HoverHeight => _hoverHeight;
        public float MaxLiftHeight => _maxLiftHeight;
        public float TowLinearDamping => _towLinearDamping;
        public float TowAngularDamping => _towAngularDamping;
        public float SnapStretch => _snapStretch;
        public float SnapGrace => _snapGrace;
        public float SnapDistance => _snapDistance;
        public float SnapCarry => _snapCarry;
        public int BeamPoints => _beamPoints;
        public float BeamArc => _beamArc;
        public float Wobble => _wobble;
        public float WobbleFrequency => _wobbleFrequency;
        public float StrainWobble => _strainWobble;
        public Vector2 BeamWidth => _beamWidth;
        public float BeamExtend => _beamExtend;
        public float BeamRetract => _beamRetract;
        public float BeamIntensity => _beamIntensity;
    }
}
