using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Cargo (docs/features/M3-25, opening-v2 §5): the slots of 07's hold (empty at a new game, opening-v3 §4), the
    /// pieces held on their wrecks while the hold is full, and the sealed containers. Pure data.
    /// </summary>
    [CreateAssetMenu(menuName = "MoonProject/Gameplay/Cargo Tuning", fileName = "CargoTuning")]
    public sealed class CargoTuning : ScriptableObject
    {
        [Header("07's hold (§2.2)")]
        [Tooltip("Slots of 07's own body hold.")]
        [Range(1, 32)] [SerializeField] private int _holdSlots = 8;

        [Header("Held pieces (§2.3, 0.5.5 §2)")]
        [Tooltip("Seconds between a held piece's slow breaths of its amber halo.")]
        [Range(1f, 6f)] [SerializeField] private float _bundlePulseSeconds = 3f;

        [Tooltip("How much a held piece breathes on each slow pulse (0.08 = 8% of the halo's glow).")]
        [Range(0f, 0.3f)] [SerializeField] private float _bundlePulse = 0.08f;

        [Tooltip("Glow of a held piece's halo shell on its wreck: it waits there, cut, for room in the hold.")]
        [Range(0f, 2f)] [SerializeField] private float _heldHalo = 0.6f;

        [Tooltip("Colour of a held piece's halo shell (HDR, linear): amber, apart from the cyan of a piece to cut.")]
        [ColorUsage(false, true)] [SerializeField] private Color _heldHaloColor = new Color(1f, 0.55f, 0.16f, 1f);

        [Tooltip("Held pieces that glint from afar at once (more still wait; they just do not glint).")]
        [Range(1, 64)] [SerializeField] private int _bundleGlints = 16;

        [Header("Sealed containers (0.5.5 §2)")]
        [Tooltip("How big a container's one cut is (its seal).")]
        [SerializeField] private PieceSize _containerCut = PieceSize.Small;

        [Tooltip("Degrees off 07's heading within which a sealed container in reach claims E: one beside 07 is cut " +
                 "without backing round to it, one behind leaves E to what 07 faces (a scan of a panel near a crate).")]
        [Range(20f, 180f)] [SerializeField] private float _containerClaimAngle = 90f;

        [Tooltip("Metres (horizontal) from a sealed container's seal within which it claims E: 07 cuts a seal " +
                 "standing beside the crate, so a crate across the clearing leaves E to the scan or piece 07 is at.")]
        [Range(1f, 8f)] [SerializeField] private float _containerReach = 3.5f;

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

        [Tooltip("How much squarer (cosine of the angle off 07's heading) another seal must face before the one offered " +
                 "now gives way to it, so two seals side by side do not flicker.")]
        [Range(0f, 0.5f)] [SerializeField] private float _containerStickyFacing = 0.08f;

        public PieceSize ContainerCut => _containerCut;
        public float ContainerClaimAngle => _containerClaimAngle;

        public float ContainerStickyFacing => _containerStickyFacing;
        public float ContainerReach => _containerReach;
        public float ContainerLidSwing => _containerLidSwing;
        public float ContainerLidSeconds => _containerLidSeconds;
        public float ContainerSealGlow => _containerSealGlow;
        public float ContainerSealNearGlow => _containerSealNearGlow;

        public Vector2 ContainerSealFade => _containerSealFade.x <= _containerSealFade.y
            ? _containerSealFade
            : new Vector2(_containerSealFade.y, _containerSealFade.x);

        public int HoldSlots => _holdSlots;
        public float BundlePulseSeconds => _bundlePulseSeconds;
        public float BundlePulse => _bundlePulse;
        public float HeldHalo => _heldHalo;
        public Color HeldHaloColor => _heldHaloColor;
        public int BundleGlints => _bundleGlints;
    }
}
