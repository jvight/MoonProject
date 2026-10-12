using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Cargo and storage (docs/features/M3-25): the slots of 07's hold and home's shelves, the dock's unload, waiting
    /// bundles, Tilly's fetch and the crew's supply crates. Pure data.
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

        [Header("Home's shelves (§3.1)")]
        [Tooltip("Slots of one shelf.")]
        [Range(1, 32)] [SerializeField] private int _shelfSlots = 12;

        [Tooltip("Shelves that come with the base.")]
        [Range(1, 4)] [SerializeField] private int _builtInShelves = 2;

        [Tooltip("Free sockets round the belly ring where more shelves can be built.")]
        [Range(0, 4)] [SerializeField] private int _shelfSockets = 2;

        [Header("Unloading at the dock (§3.2)")]
        [Tooltip("Seconds from the dock locking to the hatch opening and the first crate moving.")]
        [Range(0.1f, 2f)] [SerializeField] private float _unloadLeadSeconds = 0.5f;

        [Tooltip("Seconds between crates riding the belt onto the shelves.")]
        [Range(0.05f, 0.5f)] [SerializeField] private float _unloadStepSeconds = 0.15f;

        [Header("Waiting bundles (§2.3)")]
        [Tooltip("Metres from 07 where a bundle that did not fit settles (beside 07, never under it).")]
        [Range(1f, 6f)] [SerializeField] private float _bundleSettleDistance = 2.6f;

        [Tooltip("Seconds between a waiting bundle's slow cyan pulses.")]
        [Range(1f, 6f)] [SerializeField] private float _bundlePulseSeconds = 3f;

        [Tooltip("Metres a waiting bundle hovers over the ground.")]
        [Range(0f, 1.5f)] [SerializeField] private float _bundleHover = 0.45f;

        [Tooltip("Seconds a bundle that bumped the closed hatch takes to settle beside 07.")]
        [Range(0.2f, 2f)] [SerializeField] private float _bundleSettleSeconds = 0.7f;

        [Tooltip("How much a waiting bundle swells on each slow pulse (0.08 = 8%).")]
        [Range(0f, 0.3f)] [SerializeField] private float _bundlePulse = 0.08f;

        [Tooltip("Waiting bundles that glint from afar at once (more still wait; they just do not glint).")]
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
        [Tooltip("Units of the site's signature material in each crate.")]
        [Range(1, 12)] [SerializeField] private int _crateUnits = 6;

        public int HoldSlots => _holdSlots;
        public int CradleSlots => _cradleSlots;
        public int SideBinSlots => _sideBinSlots;
        public int ShelfSlots => _shelfSlots;
        public int BuiltInShelves => _builtInShelves;
        public int ShelfSockets => _shelfSockets;
        public float UnloadLeadSeconds => _unloadLeadSeconds;
        public float UnloadStepSeconds => _unloadStepSeconds;
        public float BundleSettleDistance => _bundleSettleDistance;
        public float BundlePulseSeconds => _bundlePulseSeconds;
        public float BundleHover => _bundleHover;
        public float BundleSettleSeconds => _bundleSettleSeconds;
        public float BundlePulse => _bundlePulse;
        public int BundleGlints => _bundleGlints;
        public float FetchIntervalSeconds => _fetchIntervalSeconds;
        public float FetchSpeed => _fetchSpeed;
        public float FetchHeight => _fetchHeight;
        public string FetchFriend => _fetchFriend;
        public float FetchHang => _fetchHang;
        public float FetchArrive => _fetchArrive;
        public int CrateUnits => _crateUnits;

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
