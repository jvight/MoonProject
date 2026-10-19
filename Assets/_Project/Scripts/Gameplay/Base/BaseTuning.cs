using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The dock lock on a charging pad 07 built (docs/design/feel-pass.md §7.1, opening-v2 §5): how its walls rise,
    /// clamp and lower, and how its strips fill with 07's cells; and how the base's lamps come on at dusk and go off
    /// at dawn. Created by the Gameplay/Tuning builder; runtime code only reads it.
    /// </summary>
    public sealed class BaseTuning : ScriptableObject
    {
        [Header("Dock lock (feel-pass §7.1)")]
        [Tooltip("Seconds between one wall starting to rise (or lower) and the next.")]
        [Range(0f, 0.3f)] [SerializeField] private float _wallStagger = 0.06f;

        [Tooltip("Seconds each wall takes to rise when 07 docks to charge.")]
        [Range(0.1f, 1.5f)] [SerializeField] private float _wallRiseSeconds = 0.45f;

        [Tooltip("How far past its top a rising wall overshoots before it settles (ease-out back).")]
        [Range(0f, 3f)] [SerializeField] private float _wallOvershoot = 1.2f;

        [Tooltip("Seconds each wall takes to lower once 07 is full (ease-in).")]
        [Range(0.1f, 2f)] [SerializeField] private float _wallLowerSeconds = 0.6f;

        [Tooltip("Seconds the walls take to drop when 07 drives off before it is full.")]
        [Range(0.05f, 1f)] [SerializeField] private float _wallDropSeconds = 0.25f;

        [Tooltip("Metres a lowered wall sinks below its raised pose, hidden in its slot.")]
        [Range(0.2f, 1f)] [SerializeField] private float _wallSink = 0.42f;

        [Tooltip("Seconds the strips take to sweep from cyan to warm gold when 07 is full, before the walls lower.")]
        [Range(0.1f, 2f)] [SerializeField] private float _fullSweepSeconds = 0.7f;

        [Tooltip("Glow of a wall's strip where its cells are charged.")]
        [Range(0f, 4f)] [SerializeField] private float _stripLit = 3.2f;

        [Tooltip("Glow of a raised wall's strip where its cells are not charged yet.")]
        [Range(0f, 1f)] [SerializeField] private float _stripUnlit = 0.12f;

        [Tooltip("Seconds (time constant) for a strip's glow to follow its cells.")]
        [Range(0f, 1f)] [SerializeField] private float _stripEase = 0.15f;

        [Tooltip("The strips' charging tint (a linear multiplier of the lamp glass's warm emission: blue needs a " +
                 "large gain).")]
        [SerializeField] private Color _stripCharging = new Color(0.12f, 1.6f, 14f, 1f);

        [Tooltip("The strips' full tint, warm gold (a linear multiplier of the lamp glass's warm emission: blue " +
                 "needs a large gain).")]
        [SerializeField] private Color _stripFull = new Color(1f, 0.95f, 0.8f, 1f);

        [Tooltip("Seconds for the rim lamps' chase to run from the outer ring to the inner one as 07 rolls on.")]
        [Range(0.2f, 2f)] [SerializeField] private float _rimChaseSeconds = 0.6f;

        [Tooltip("Metres from the pad's centre line 07 may rest off and still count as settled onto the dock.")]
        [Range(0.005f, 0.2f)] [SerializeField] private float _dockSettledMetres = 0.03f;

        [Tooltip("Degrees off the pad's heading 07 may rest at and still count as settled onto the dock.")]
        [Range(0.2f, 10f)] [SerializeField] private float _dockSettledDegrees = 1f;

        [Tooltip("Throttle (0..1, forward or back) beyond which 07, settled on the dock, is let go at once: a clear push " +
                 "to drive off. Steering alone and a light touch let nothing go, and nothing does while 07 settles.")]
        [Range(0.1f, 0.9f)] [SerializeField] private float _dockDriveOff = 0.4f;

        [Tooltip("Seconds the lock holds 07 at least before it lets go at full: a pad that takes 07 in already full still " +
                 "closes its arms (the thunk) and plays its full beat before it opens.")]
        [Range(0.2f, 3f)] [SerializeField] private float _lockDwellSeconds = 0.8f;

        [Tooltip("Speed (m/s) at or under which 07 counts as resting on the pad: a full battery is then taken in too.")]
        [Range(0.05f, 1f)] [SerializeField] private float _dockRestSpeed = 0.3f;

        [Tooltip("Seconds 07 must rest on the plate before a pad takes it in with a battery already full.")]
        [Range(0.1f, 2f)] [SerializeField] private float _dockRestSeconds = 0.5f;

        [Tooltip("Degrees 07's heading on a charging pad lies from the pad's forward axis: 180 noses 07 in toward the " +
                 "pad's cooling tower at its rear, so the tower never stands between the chase camera and 07.")]
        [Range(-180f, 180f)] [SerializeField] private float _dockHeadingOffset = 180f;

        [Header("Pad lock: chocks and clamp arms")]
        [Tooltip("Degrees the four wheel chocks hinge up from lying flush in the plate to cradle 07's wheels.")]
        [Range(10f, 90f)] [SerializeField] private float _chockLockDegrees = 65f;

        [Tooltip("Degrees the two side clamp arms swing up and in from lying flat to rest against 07's flanks.")]
        [Range(10f, 90f)] [SerializeField] private float _clampArmLockDegrees = 80f;

        [Tooltip("Seconds the clamp arms take to close once the chocks stand (the soft thunk is where they meet 07).")]
        [Range(0.1f, 1.5f)] [SerializeField] private float _clampArmCloseSeconds = 0.45f;

        [Tooltip("Seconds the clamp arms take to open when the lock lets 07 go (they open before the chocks lower).")]
        [Range(0.1f, 1.5f)] [SerializeField] private float _clampArmOpenSeconds = 0.35f;

        [Header("Base lamps")]
        [Tooltip("Degrees above last light the Sun may stand and the base's lamps still begin to come on: dusk falls " +
                 "over the base before the Sun sets.")]
        [Range(0f, 20f)] [SerializeField] private float _lampDuskAboveLastLightDegrees = 4f;

        [Tooltip("Degrees above the lamps' dusk elevation the Sun must climb before they go off again, so a Sun " +
                 "hovering at the threshold does not flicker the lamps.")]
        [Range(0.2f, 10f)] [SerializeField] private float _lampDawnMarginDegrees = 1.5f;

        [Tooltip("Seconds between one lamp starting to come on and the next, in the base's order.")]
        [Range(0f, 5f)] [SerializeField] private float _lampStaggerSeconds = 0.9f;

        [Tooltip("Seconds a lamp takes to ease up to full, like an old lamp warming.")]
        [Range(0.2f, 5f)] [SerializeField] private float _lampRiseSeconds = 1.5f;

        [Tooltip("Seconds the lamps take to ease off at dawn.")]
        [Range(0.2f, 8f)] [SerializeField] private float _lampFallSeconds = 2.5f;

        [Tooltip("How deep (0..1 of the lamp's glow) the warm flicker as a lamp catches dips it.")]
        [Range(0f, 1f)] [SerializeField] private float _lampFlickerDepth = 0.4f;

        [Tooltip("Flickers a second while a lamp catches.")]
        [Range(1f, 20f)] [SerializeField] private float _lampFlickerHertz = 6f;

        [Tooltip("Seconds from the start of a lamp's rise that the flicker lasts, dying away (within the rise).")]
        [Range(0.1f, 2f)] [SerializeField] private float _lampFlickerSeconds = 0.7f;

        [Tooltip("Structure ids in the order the lamps come on at dusk; any other structure lights after them.")]
        [SerializeField] private string[] _lampOrder =
            { "charging_pad", "solar_array", "battery_bank", "antenna_mast" };

        [Tooltip("How much brighter than a finished piece's standby glow a lit lamp's bulb burns (a multiplier of its " +
                 "emission), so the bulbs read from across the base.")]
        [Range(1f, 8f)] [SerializeField] private float _lampEmissionGain = 3.5f;

        [Tooltip("The warm tungsten amber of the pool of light a lit lamp throws on the regolith.")]
        [SerializeField] private Color _lampLightColor = new Color(1f, 0.62f, 0.28f, 1f);

        [Tooltip("Metres the warm light of a lit lamp reaches.")]
        [Range(1f, 10f)] [SerializeField] private float _lampLightRange = 4.5f;

        [Tooltip("Intensity of a lamp's warm light at full glow (it follows the lamp as it eases on and off).")]
        [Range(0f, 30f)] [SerializeField] private float _lampLightIntensity = 8f;

        [Tooltip("How high the light hangs, as a share of the piece's height.")]
        [Range(0.2f, 1f)] [SerializeField] private float _lampLightHeightShare = 0.85f;

        [Tooltip("The charging pad's light is a spot pointing down at the pad: its cone, in degrees.")]
        [Range(40f, 160f)] [SerializeField] private float _padSpotAngleDegrees = 120f;

        [Tooltip("How high above the deck the charging pad's spot hangs, in metres: the pad is a low plate, so its " +
                 "light needs a mast to pool on the plate.")]
        [Range(0.5f, 5f)] [SerializeField] private float _padSpotHeightMetres = 2.4f;

        [Tooltip("The most lamp lights the base burns at once (none casts a shadow); the first lamps in the lighting " +
                 "order get them, the rest only glow.")]
        [Range(0, 8)] [SerializeField] private int _lampLightBudget = 4;

        public float ChockLockDegrees => _chockLockDegrees;
        public float ClampArmLockDegrees => _clampArmLockDegrees;
        public float ClampArmCloseSeconds => _clampArmCloseSeconds;
        public float ClampArmOpenSeconds => _clampArmOpenSeconds;
        public float LampEmissionGain => _lampEmissionGain;
        public Color LampLightColor => _lampLightColor;
        public float LampLightRange => _lampLightRange;
        public float LampLightIntensity => _lampLightIntensity;
        public float LampLightHeightShare => _lampLightHeightShare;
        public float PadSpotAngleDegrees => _padSpotAngleDegrees;
        public float PadSpotHeightMetres => _padSpotHeightMetres;
        public int LampLightBudget => _lampLightBudget;
        public float LampDuskAboveLastLightDegrees => _lampDuskAboveLastLightDegrees;
        public float LampDawnMarginDegrees => _lampDawnMarginDegrees;
        public float LampStaggerSeconds => _lampStaggerSeconds;
        public float LampRiseSeconds => _lampRiseSeconds;
        public float LampFallSeconds => _lampFallSeconds;
        public float LampFlickerDepth => _lampFlickerDepth;
        public float LampFlickerHertz => _lampFlickerHertz;
        public float LampFlickerSeconds => Mathf.Min(_lampFlickerSeconds, _lampRiseSeconds);
        public System.Collections.Generic.IReadOnlyList<string> LampOrder => _lampOrder;
        public float LockDwellSeconds => _lockDwellSeconds;
        public float DockRestSpeed => _dockRestSpeed;
        public float DockRestSeconds => _dockRestSeconds;
        public float DockHeadingOffset => _dockHeadingOffset;
        public float DockSettledMetres => _dockSettledMetres;
        public float DockSettledDegrees => _dockSettledDegrees;
        public float DockDriveOff => _dockDriveOff;
        public float WallStagger => _wallStagger;
        public float WallRiseSeconds => _wallRiseSeconds;
        public float WallOvershoot => _wallOvershoot;
        public float WallLowerSeconds => _wallLowerSeconds;
        public float WallDropSeconds => _wallDropSeconds;
        public float WallSink => _wallSink;
        public float FullSweepSeconds => _fullSweepSeconds;
        public float StripLit => _stripLit;
        public float StripUnlit => _stripUnlit;
        public float StripEase => _stripEase;
        public Color StripCharging => _stripCharging;
        public Color StripFull => _stripFull;
        public float RimChaseSeconds => _rimChaseSeconds;
    }
}
