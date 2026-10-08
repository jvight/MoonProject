using System;
using UnityEngine;
using MoonProject.Core;
using MoonProject.Core.Events;

namespace MoonProject.Rover
{
    /// <summary>
    /// 07's visible progression (VISION ruling 11, docs/features/M3-11): the kit pieces its abilities bring and the
    /// gifts its friends give, each a node hidden until earned.
    /// <list type="bullet">
    /// <item>Kit by <see cref="IRoverAbilities"/>: the Warm Headlamp's lamp bar, the Boost Coils' capacitor drums, the
    /// Cargo Cradle's rear rack, and when the Hover-Jump's coils may appear (<see cref="Shows"/>; they pop in through
    /// <see cref="RoverHoverCoils"/>). Owned when the game loads: there at once. Bought (an UpgradePurchased for a
    /// "rover." id): <see cref="RoverKitInstalling"/>, then the piece appears above its socket, drops and settles
    /// (<see cref="KitFit"/>) with <see cref="RoverKitFitted"/> as it lands.</item>
    /// <item>Gifts by <see cref="IFriendRoster"/> (<see cref="FriendGift"/>): Tilly's cell for the solar wing, Bell's
    /// fresh "07" and pennant. A friend repaired before the game loaded: there at once; repaired during play: the
    /// soft version of the moment the next time 07 is home.</item>
    /// <item>The road light: the lamp bar makes it wider and warmer and moves it to the bar's middle glass, and its
    /// three glasses glow; the drums' bands glow with the boost.</item>
    /// </list>
    /// Registered by <see cref="RoverController"/> as <see cref="IRoverCargoSeat"/> (the rack's RelicSeat). Ticked by
    /// <see cref="RoverController"/> in Update, so the rack has its pose for this frame before any LateUpdate reads it.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class RoverKit : MonoBehaviour, IRoverCargoSeat
    {
        private const int PieceCount = 6;
        private const int DrumCount = 2;

        /// <summary>Glow changes smaller than this are not worth a new property block.</summary>
        private const float GlowEpsilon = 1e-4f;

        private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");

        [Tooltip("Rig tuning (Assets/_Project/Data/Tuning/RoverRigTuning.asset): 'Visible kit and gifts', 'Headlamp'.")]
        [SerializeField] private RoverRigTuning _tuning;

        [Tooltip("Warm spot light under RoverModel 'HeadlampSocket' (07's road light).")]
        [SerializeField] private Light _headlamp;

        [Tooltip("Art's Kit_LampBar under HeadlampSocket (inactive until the Warm Headlamp is owned).")]
        [SerializeField] private Transform _lampBar;

        [Tooltip("The lamp bar's glasses Lamp_0..2 (glow renderers on M_LowPolyGlowOff).")]
        [SerializeField] private Renderer[] _lamps = new Renderer[RoverModelNodes.KitLampCount];

        [Tooltip("Art's Kit_CapacitorDrum under DrumSocket_L and DrumSocket_R, in that order (inactive until owned).")]
        [SerializeField] private Transform[] _drums = new Transform[DrumCount];

        [Tooltip("Each drum's Glow band (glow renderer on M_LowPolyGlowOff, cyan), in the same order.")]
        [SerializeField] private Renderer[] _drumGlows = new Renderer[DrumCount];

        [Tooltip("Art's Kit_CargoRack under CargoSocket (inactive until the Cargo Cradle is owned).")]
        [SerializeField] private Transform _cargoRack;

        [Tooltip("The rack's RelicSeat, where a carried relic rests (+Y up).")]
        [SerializeField] private Transform _relicSeat;

        [Tooltip("RoverModel 'SolarWing/CellFilled': Tilly's replacement cell (hidden until her gift).")]
        [SerializeField] private Transform _solarCell;

        [Tooltip("RoverModel 'Body/Decal07Fresh': Ro's fresh '07' (hidden until Bell's gift).")]
        [SerializeField] private Transform _freshSerial;

        [Tooltip("RoverModel 'Antenna/Pennant': Bell's radio pennant (hidden until her gift).")]
        [SerializeField] private Transform _pennant;

        private readonly KitFit[] _fits = new KitFit[PieceCount];
        private readonly FriendGift _tillyGift = new FriendGift();
        private readonly FriendGift _bellGift = new FriendGift();
        private RoverController _rover;
        private GameContext _context;
        private EventBus _events;
        private IWorldLayout _world;
        private IFriendState _tilly;
        private IFriendState _bell;
        private IDisposable _purchases;
        private MaterialPropertyBlock _glowBlock;
        private Vector3 _lampBarRest;
        private Vector3[] _drumRest;
        private Vector3 _rackRest;
        private Vector3 _lampBarScale;
        private Vector3[] _drumScale;
        private Vector3 _rackScale;
        private Vector3 _cellScale;
        private Vector3 _serialScale;
        private Vector3 _pennantScale;
        private float _baseIntensity;
        private float _baseRange;
        private float _baseSpotAngle;
        private float _baseInnerSpotAngle;
        private Color _baseColor;
        private Vector3 _baseLightPosition;
        private Vector3 _warmLightPosition;
        private float _appliedWarmth = -1f;
        private float _appliedDrumGlow = -1f;
        private bool _purchased;
        private bool _started;

        /// <summary>True when the piece is on 07 right now (settling in or fitted).</summary>
        public bool Shows(RoverKitPiece piece)
        {
            return _fits[(int)piece].Visible;
        }

        /// <summary>The fit of <paramref name="piece"/> (for tests and tooling).</summary>
        public KitFit Fit(RoverKitPiece piece)
        {
            return _fits[(int)piece];
        }

        // IRoverCargoSeat
        public bool IsFitted => _rover.Has(RoverAbility.CargoCradle);

        public Vector3 Position => _relicSeat.position;

        public Quaternion Rotation => _relicSeat.rotation;

        /// <summary>Checks wiring, hides all kit and gifts, sets the road light; false when broken (logged).</summary>
        public bool Initialize(GameContext context, RoverController rover)
        {
            if (!ValidateWiring())
            {
                enabled = false;
                return false;
            }

            _rover = rover;
            _context = context;
            _events = context.Events;
            _world = context.Get<IWorldLayout>();
            KitSettings settings = _tuning.Kit;
            for (int i = 0; i < PieceCount; i++)
            {
                RoverKitPiece piece = (RoverKitPiece)i;
                _fits[i] = new KitFit(settings, piece == RoverKitPiece.SolarCell || piece == RoverKitPiece.FreshPaint);
            }

            _glowBlock = new MaterialPropertyBlock();
            CacheRestPoses();
            ConfigureHeadlamp();
            HideAll();
            _purchases = _events.Subscribe<UpgradePurchased>(OnUpgradePurchased);
            return true;
        }

        private bool ValidateWiring()
        {
            bool ok = Require(_tuning != null, "RoverRigTuning is not assigned.")
                & Require(_headlamp != null, "Headlamp light is not assigned.")
                & Require(_lampBar != null && _cargoRack != null && _relicSeat != null,
                    "Lamp bar, cargo rack or its RelicSeat is not assigned.")
                & Require(_solarCell != null && _freshSerial != null && _pennant != null,
                    "A gift node (CellFilled, Decal07Fresh, Pennant) is not assigned.")
                & Require(AllSet(_lamps, RoverModelNodes.KitLampCount), "The lamp bar needs its three glasses.")
                & Require(AllSet(_drums, DrumCount) && AllSet(_drumGlows, DrumCount),
                    "Both capacitor drums and their glow bands are required.");
            return ok;
        }

        private static bool AllSet<T>(T[] items, int count) where T : UnityEngine.Object
        {
            if (items == null || items.Length != count)
            {
                return false;
            }

            for (int i = 0; i < items.Length; i++)
            {
                if (items[i] == null)
                {
                    return false;
                }
            }

            return true;
        }

        private bool Require(bool condition, string message)
        {
            if (!condition)
            {
                Debug.LogError($"{nameof(RoverKit)}: {message}", this);
            }

            return condition;
        }

        private void CacheRestPoses()
        {
            _lampBarRest = _lampBar.localPosition;
            _lampBarScale = _lampBar.localScale;
            _drumRest = new Vector3[DrumCount];
            _drumScale = new Vector3[DrumCount];
            for (int i = 0; i < DrumCount; i++)
            {
                _drumRest[i] = _drums[i].localPosition;
                _drumScale[i] = _drums[i].localScale;
            }

            _rackRest = _cargoRack.localPosition;
            _rackScale = _cargoRack.localScale;
            _cellScale = _solarCell.localScale;
            _serialScale = _freshSerial.localScale;
            _pennantScale = _pennant.localScale;
        }

        /// <summary>The plain road light from the rig tuning, and where the lamp bar's middle glass moves it.</summary>
        private void ConfigureHeadlamp()
        {
            _headlamp.type = LightType.Spot;
            _baseIntensity = _tuning.HeadlampIntensity;
            _baseRange = _tuning.HeadlampRange;
            _baseSpotAngle = _tuning.HeadlampSpotAngle;
            _baseInnerSpotAngle = _tuning.HeadlampInnerSpotAngle;
            _baseColor = _headlamp.color;
            _baseLightPosition = _headlamp.transform.localPosition;
            Transform socket = _headlamp.transform.parent;
            Vector3 middleGlass = _lamps[RoverModelNodes.KitLampCount / 2].transform.position;
            _warmLightPosition = socket.InverseTransformPoint(middleGlass);
            ApplyWarmth(0f);
        }

        private void HideAll()
        {
            _lampBar.gameObject.SetActive(false);
            for (int i = 0; i < DrumCount; i++)
            {
                _drums[i].gameObject.SetActive(false);
            }

            _cargoRack.gameObject.SetActive(false);
            _solarCell.gameObject.SetActive(false);
            _freshSerial.gameObject.SetActive(false);
            _pennant.gameObject.SetActive(false);
        }

        private void OnUpgradePurchased(UpgradePurchased upgrade)
        {
            _purchased |= RoverKitPieces.IsRoverUpgrade(upgrade.UpgradeId);
        }

        public void Tick(float deltaTime)
        {
            if (!_started)
            {
                FindFriends();
            }

            for (int i = 0; i < PieceCount; i++)
            {
                var piece = (RoverKitPiece)i;
                if (RoverKitPieces.TryGetAbility(piece, out RoverAbility ability) && _rover.Has(ability)
                    && _fits[i].IsHidden)
                {
                    Bring(piece, _started && _purchased, false);
                }
            }

            bool home = IsHome();
            Give(_tillyGift.Step(IsAwake(_tilly), home), RoverKitPiece.SolarCell);
            Give(_bellGift.Step(IsAwake(_bell), home), RoverKitPiece.FreshPaint);

            for (int i = 0; i < PieceCount; i++)
            {
                if (_fits[i].Step(deltaTime))
                {
                    var piece = (RoverKitPiece)i;
                    _events.Publish(new RoverKitFitted(piece, piece == RoverKitPiece.SolarCell
                        || piece == RoverKitPiece.FreshPaint));
                }
            }

            ApplyPieces();
            ApplyWarmth(_fits[(int)RoverKitPiece.LampBar].Lights);
            ApplyDrumGlow();
            _purchased = false;
            _started = true;
        }

        /// <summary>Tilly's cell has been given (the solar wing then opens wider at rest).</summary>
        public bool HasMendedWing => _tillyGift.Given;

        private void FindFriends()
        {
            if (!_context.TryGet(out IFriendRoster roster))
            {
                Debug.LogError($"{nameof(RoverKit)}: no {nameof(IFriendRoster)} is registered (Gameplay); friends' "
                    + "gifts cannot show.", this);
                return;
            }

            for (int i = 0; i < roster.Count; i++)
            {
                IFriendState friend = roster.Get(i);
                if (friend.Id == RoverKitPieces.TillyId)
                {
                    _tilly = friend;
                }
                else if (friend.Id == RoverKitPieces.BellId)
                {
                    _bell = friend;
                }
            }

            if (_tilly == null || _bell == null)
            {
                Debug.LogError($"{nameof(RoverKit)}: the friend roster lacks '{RoverKitPieces.TillyId}' or "
                    + $"'{RoverKitPieces.BellId}'; their gifts cannot show.", this);
            }
        }

        private static bool IsAwake(IFriendState friend)
        {
            return friend != null && friend.Activity != FriendActivity.Dormant
                && friend.Activity != FriendActivity.Repairing;
        }

        private bool IsHome()
        {
            KitSettings settings = _tuning.Kit;
            Vector3 offset = _rover.Position - _world.BasePosition;
            float radius = settings.GiftHomeRadius;
            return offset.x * offset.x + offset.z * offset.z <= radius * radius
                && _rover.Speed <= settings.GiftMaxSpeed;
        }

        private void Give(GiftCue cue, RoverKitPiece piece)
        {
            if (cue == GiftCue.ShowSilently)
            {
                Bring(piece, false, true);
            }
            else if (cue == GiftCue.Present)
            {
                Bring(piece, true, true);
            }
        }

        /// <summary>Shows <paramref name="piece"/> at once, or installs it with the moment.</summary>
        private void Bring(RoverKitPiece piece, bool withMoment, bool gift)
        {
            KitFit fit = _fits[(int)piece];
            if (!withMoment)
            {
                fit.Show();
                return;
            }

            fit.Install();
            _events.Publish(new RoverKitInstalling(piece, gift));
        }

        private void ApplyPieces()
        {
            Place(_lampBar, _fits[(int)RoverKitPiece.LampBar], _lampBarRest, _lampBarScale);
            KitFit drums = _fits[(int)RoverKitPiece.CapacitorDrums];
            for (int i = 0; i < DrumCount; i++)
            {
                Place(_drums[i], drums, _drumRest[i], _drumScale[i]);
            }

            Place(_cargoRack, _fits[(int)RoverKitPiece.CargoRack], _rackRest, _rackScale);
            Grow(_solarCell, _fits[(int)RoverKitPiece.SolarCell], _cellScale);
            KitFit paint = _fits[(int)RoverKitPiece.FreshPaint];
            Grow(_freshSerial, paint, _serialScale);
            Grow(_pennant, paint, _pennantScale);
        }

        /// <summary>
        /// A kit piece at its socket, raised along world up by the fit's drop and scaled by its grow (written only
        /// while it moves or as it appears).
        /// </summary>
        private static void Place(Transform piece, KitFit fit, Vector3 rest, Vector3 scale)
        {
            if (!Reveal(piece, fit))
            {
                return;
            }

            Transform socket = piece.parent;
            piece.localPosition = rest + socket.InverseTransformDirection(Vector3.up) * fit.Offset;
            piece.localScale = scale * fit.Scale;
        }

        /// <summary>A gift node shown and grown by its fit.</summary>
        private static void Grow(Transform gift, KitFit fit, Vector3 scale)
        {
            if (Reveal(gift, fit))
            {
                gift.localScale = scale * fit.Scale;
            }
        }

        /// <summary>Shows or hides <paramref name="node"/> with its fit; true when its pose needs writing.</summary>
        private static bool Reveal(Transform node, KitFit fit)
        {
            bool visible = fit.Visible;
            bool changed = node.gameObject.activeSelf != visible;
            if (changed)
            {
                node.gameObject.SetActive(visible);
            }

            return visible && (changed || fit.Moving);
        }

        /// <summary>The road light and the lamp bar's glasses, <paramref name="warmth"/> of the way to warm.</summary>
        private void ApplyWarmth(float warmth)
        {
            if (Mathf.Abs(warmth - _appliedWarmth) < GlowEpsilon)
            {
                return;
            }

            KitSettings settings = _tuning.Kit;
            _headlamp.intensity = Mathf.Lerp(_baseIntensity, settings.WarmIntensity, warmth);
            _headlamp.range = Mathf.Lerp(_baseRange, settings.WarmRange, warmth);
            _headlamp.spotAngle = Mathf.Lerp(_baseSpotAngle, settings.WarmSpotAngle, warmth);
            _headlamp.innerSpotAngle = Mathf.Lerp(_baseInnerSpotAngle, settings.WarmInnerSpotAngle, warmth);
            _headlamp.color = Color.Lerp(_baseColor, settings.WarmColor, warmth);
            _headlamp.transform.localPosition = Vector3.Lerp(_baseLightPosition, _warmLightPosition, warmth);
            SetGlow(_lamps, settings.LampBarGlow * warmth);
            _appliedWarmth = warmth;
        }

        private void ApplyDrumGlow()
        {
            KitSettings settings = _tuning.Kit;
            float lit = _fits[(int)RoverKitPiece.CapacitorDrums].Lights;
            float glow = lit * Mathf.Lerp(settings.DrumIdleGlow, settings.DrumGlow, _rover.BoostLevel);
            if (Mathf.Abs(glow - _appliedDrumGlow) < GlowEpsilon)
            {
                return;
            }

            SetGlow(_drumGlows, glow);
            _appliedDrumGlow = glow;
        }

        private void SetGlow(Renderer[] renderers, float intensity)
        {
            _glowBlock.SetVector(EmissionColorId, new Vector4(intensity, intensity, intensity, 1f));
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].SetPropertyBlock(_glowBlock);
            }
        }

        private void OnDestroy()
        {
            _purchases?.Dispose();
        }
    }
}
