using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Cargo and storage (docs/features/M3-25): the slots of 07's hold and home's shelves, the dock's unload, waiting
    /// bundles and Tilly's fetch. Pure data.
    /// </summary>
    [CreateAssetMenu(menuName = "MoonProject/Gameplay/Cargo Tuning", fileName = "CargoTuning")]
    public sealed class CargoTuning : ScriptableObject
    {
        [Header("07's hold (§2.2)")]
        [Tooltip("Slots of 07's own body hold.")]
        [Range(1, 32)] [SerializeField] private int _holdSlots = 8;

        [Tooltip("Slots the Cargo Cradle's straps add once it is fitted.")]
        [Range(0, 8)] [SerializeField] private int _cradleSlots = 2;

        [Tooltip("Slots the Side Bins add once they are fitted.")]
        [Range(0, 12)] [SerializeField] private int _sideBinSlots = 4;

        [Tooltip("07's spares: what its hold carries at the start of a new game (0.5.5 D4). A loaded save keeps its own " +
                 "hold instead.")]
        [SerializeField] private Recipe _starterItems = new Recipe(2, 1, 0);

        [Header("Home's shelves (§3.1)")]
        [Tooltip("Slots of one shelf.")]
        [Range(1, 32)] [SerializeField] private int _shelfSlots = 12;

        [Tooltip("Shelves that come with the base.")]
        [Range(1, 4)] [SerializeField] private int _builtInShelves = 2;

        [Tooltip("Free sockets round the belly ring where more shelves can be built.")]
        [Range(0, 4)] [SerializeField] private int _shelfSockets = 2;

        [Tooltip("What each free socket's shelf takes, in socket order (M3-25 §3.1: shelf 3, then shelf 4).")]
        [SerializeField] private Recipe[] _shelfCosts = { new Recipe(3, 0, 0), new Recipe(4, 1, 0) };

        [Tooltip("Metres from a free socket within which 07 can build its shelf.")]
        [Range(1f, 6f)] [SerializeField] private float _socketRadius = 2.5f;

        [Tooltip("Seconds 07 holds E at a socket to build its shelf.")]
        [Range(0.3f, 4f)] [SerializeField] private float _buildHoldSeconds = 1.5f;

        [Tooltip("Seconds a new shelf takes to unfold from its crate.")]
        [Range(0.3f, 4f)] [SerializeField] private float _unfoldSeconds = 1.2f;

        [Tooltip("Crates a full slot shows, stacked (fewer as it empties).")]
        [Range(1, 3)] [SerializeField] private int _cratesPerSlot = 3;

        [Tooltip("Glow of a built shelf's rack lamp once the tower powers home.")]
        [Range(0f, 4f)] [SerializeField] private float _rackLampGlow = 1f;

        [Header("Unloading at the dock (§3.2)")]
        [Tooltip("Seconds from the dock locking to the hatch opening and the first crate moving.")]
        [Range(0.1f, 2f)] [SerializeField] private float _unloadLeadSeconds = 0.5f;

        [Tooltip("Seconds between crates riding the belt onto the shelves.")]
        [Range(0.05f, 0.5f)] [SerializeField] private float _unloadStepSeconds = 0.15f;

        [Header("Held stacks (§2.3, 0.5.5 §2)")]
        [Tooltip("Seconds between a held stack's slow breaths (a piece's amber halo, a crate bundle's swell).")]
        [Range(1f, 6f)] [SerializeField] private float _bundlePulseSeconds = 3f;

        [Tooltip("Seconds what did not fit takes to glide back into its crate's mouth.")]
        [Range(0.2f, 2f)] [SerializeField] private float _bundleSettleSeconds = 0.7f;

        [Tooltip("How much a held stack breathes on each slow pulse (0.08 = 8% swell, or 8% of the halo's glow).")]
        [Range(0f, 0.3f)] [SerializeField] private float _bundlePulse = 0.08f;

        [Tooltip("Glow of a held piece's halo shell on its wreck: it waits there, cut, for room in the hold.")]
        [Range(0f, 2f)] [SerializeField] private float _heldHalo = 0.6f;

        [Tooltip("Colour of a held piece's halo shell (HDR, linear): amber, apart from the cyan of a piece to cut.")]
        [ColorUsage(false, true)] [SerializeField] private Color _heldHaloColor = new Color(1f, 0.55f, 0.16f, 1f);

        [Tooltip("Held stacks that glint from afar at once (more still wait; they just do not glint).")]
        [Range(1, 64)] [SerializeField] private int _bundleGlints = 16;

        [Header("Tilly fetches (T3)")]
        [Tooltip("The flying friend who fetches waiting bundles once she is home (her FriendDefinition id).")]
        [SerializeField] private string _fetchFriend = "tilly";

        [Tooltip("Metres under her body the hooked bundle hangs.")]
        [Range(0.2f, 3f)] [SerializeField] private float _fetchHang = 0.9f;

        [Tooltip("She has reached the bundle or the shelves within this many metres.")]
        [Range(0.3f, 4f)] [SerializeField] private float _fetchArrive = 1.2f;

        [Tooltip("Seconds between Tilly's fetches while a bundle waits inside the station's reach.")]
        [Range(5f, 120f)] [SerializeField] private float _fetchIntervalSeconds = 40f;

        [Tooltip("Tilly's calm speed carrying a bundle (m/s).")]
        [Range(1f, 10f)] [SerializeField] private float _fetchSpeed = 4f;

        [Tooltip("Metres over the ground Tilly flies while fetching.")]
        [Range(1f, 10f)] [SerializeField] private float _fetchHeight = 4f;

        [Header("The crew's supply crates (T4)")]
        [Tooltip("How big the crate's one cut is (its seal): a short, snappy cut.")]
        [SerializeField] private PieceSize _crateCut = PieceSize.Small;

        [Tooltip("Metres the lid lifts once the seal is cut.")]
        [Range(0f, 1f)] [SerializeField] private float _lidLift = 0.3f;

        [Tooltip("Degrees the lid tips back as it lifts.")]
        [Range(0f, 90f)] [SerializeField] private float _lidTilt = 35f;

        [Tooltip("Seconds the lid takes to lift.")]
        [Range(0.2f, 3f)] [SerializeField] private float _lidSeconds = 0.8f;

        [Tooltip("Glow of a sealed crate's seam (dark once cut).")]
        [Range(0f, 4f)] [SerializeField] private float _sealGlow = 1f;

        [Header("Sealed containers (0.5.5 §2)")]
        [Tooltip("How big a container's one cut is (its seal).")]
        [SerializeField] private PieceSize _containerCut = PieceSize.Small;

        [Tooltip("Degrees a container's lid swings open about its hinge once the seal is cut.")]
        [Range(30f, 180f)] [SerializeField] private float _containerLidSwing = 105f;

        [Tooltip("Seconds the lid takes to swing open; the find slides out once it stands open.")]
        [Range(0.2f, 3f)] [SerializeField] private float _containerLidSeconds = 0.9f;

        [Tooltip("Glow of a container's seal seen from afar: a small mark on the object that reads across the basin, " +
                 "never a lamp (its seal can be a whole lid face; 1 = the palette's glow as authored).")]
        [Range(0f, 1f)] [SerializeField] private float _containerSealGlow = 0.8f;

        [Tooltip("Glow of a container's seal once 07 has arrived: the mark has done its job and only whispers where " +
                 "to cut, so it never washes over 07.")]
        [Range(0f, 1f)] [SerializeField] private float _containerSealNearGlow = 0.1f;

        [Tooltip("07's distance (m) to the seal over which its glow eases from the near to the far level (near, far).")]
        [SerializeField] private Vector2 _containerSealFade = new Vector2(3f, 9f);

        public PieceSize CrateCut => _crateCut;
        public PieceSize ContainerCut => _containerCut;
        public float ContainerLidSwing => _containerLidSwing;
        public float ContainerLidSeconds => _containerLidSeconds;
        public float ContainerSealGlow => _containerSealGlow;
        public float ContainerSealNearGlow => _containerSealNearGlow;

        public Vector2 ContainerSealFade => _containerSealFade.x <= _containerSealFade.y
            ? _containerSealFade
            : new Vector2(_containerSealFade.y, _containerSealFade.x);
        public float LidLift => _lidLift;
        public float LidTilt => _lidTilt;
        public float LidSeconds => _lidSeconds;
        public float SealGlow => _sealGlow;
        public int HoldSlots => _holdSlots;
        public int CradleSlots => _cradleSlots;
        public int SideBinSlots => _sideBinSlots;
        public int ShelfSlots => _shelfSlots;
        public int BuiltInShelves => _builtInShelves;
        public Recipe StarterItems => _starterItems;
        public int ShelfSockets => _shelfSockets;
        public float SocketRadius => _socketRadius;
        public float BuildHoldSeconds => _buildHoldSeconds;
        public float UnfoldSeconds => _unfoldSeconds;
        public int CratesPerSlot => _cratesPerSlot;
        public float RackLampGlow => _rackLampGlow;

        /// <summary>What free socket <paramref name="socket"/>'s shelf takes (0 = the first free socket).</summary>
        public Recipe ShelfCost(int socket)
        {
            return _shelfCosts[Mathf.Clamp(socket, 0, _shelfCosts.Length - 1)];
        }
        public float UnloadLeadSeconds => _unloadLeadSeconds;
        public float UnloadStepSeconds => _unloadStepSeconds;
        public float BundlePulseSeconds => _bundlePulseSeconds;
        public float BundleSettleSeconds => _bundleSettleSeconds;
        public float BundlePulse => _bundlePulse;
        public float HeldHalo => _heldHalo;
        public Color HeldHaloColor => _heldHaloColor;
        public int BundleGlints => _bundleGlints;
        public float FetchIntervalSeconds => _fetchIntervalSeconds;
        public float FetchSpeed => _fetchSpeed;
        public float FetchHeight => _fetchHeight;
        public string FetchFriend => _fetchFriend;
        public float FetchHang => _fetchHang;
        public float FetchArrive => _fetchArrive;

        /// <summary>Every shelf's slots, the built-in ones first, then the sockets.</summary>
        public int[] ShelfLayout()
        {
            var slots = new int[_builtInShelves + _shelfSockets];
            for (int i = 0; i < slots.Length; i++)
            {
                slots[i] = _shelfSlots;
            }

            return slots;
        }
    }
}
