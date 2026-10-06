using UnityEngine;
using MoonProject.Art;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Home: where the lander stands, how relics are taken onto the museum shelf, the scrap gift for each memory
    /// brought home, and how the base warms up as 07 approaches. Created by the Gameplay/Tuning builder; runtime code
    /// only reads it.
    /// </summary>
    public sealed class BaseTuning : ScriptableObject
    {
        [Header("Placement")]
        [Tooltip("Lander position relative to the base pad centre (m). 07 spawns at the centre, so it stands aside, " +
                 "facing the centre, well inside the ~25 m pad.")]
        [SerializeField] private Vector3 _landerOffset = new Vector3(-10f, 0f, 8f);

        [Header("Deposit")]
        [Tooltip("A relic let go within this many metres (horizontal) of the shelf floats onto it.")]
        [Range(1f, 20f)] [SerializeField] private float _depositRadius = 7f;

        [Tooltip("...and no higher than this above the shelf's base (m).")]
        [Range(1f, 20f)] [SerializeField] private float _depositHeight = 6f;

        [Tooltip("Seconds a relic takes to float onto its slot.")]
        [Range(0.3f, 6f)] [SerializeField] private float _depositDuration = 1.9f;

        [Tooltip("Height (m) of the arc it floats on.")]
        [Range(0f, 4f)] [SerializeField] private float _depositLift = 0.9f;

        [Tooltip("It glides to this many metres above the slot, then sinks onto it.")]
        [Range(0f, 2f)] [SerializeField] private float _depositHover = 0.25f;

        [Tooltip("Overshoot of the final settle (0 = none, ~1.7 = classic back-ease).")]
        [Range(0f, 3f)] [SerializeField] private float _settleOvershoot = 1.4f;

        [Tooltip("Scrap gift for every memory brought home (no grind: exploring funds the tower).")]
        [Range(0, 100)] [SerializeField] private int _depositGift = 10;

        [Tooltip("Radius (m) of the small warm glow that shows which slot a towed relic will take.")]
        [Range(0.1f, 2f)] [SerializeField] private float _slotHintRadius = 0.2f;

        [Tooltip("Brightness of that glow.")]
        [Range(0f, 4f)] [SerializeField] private float _slotHintIntensity = 0.55f;

        [Tooltip("Seconds (time constant) for the slot hint to fade in or out.")]
        [Range(0f, 2f)] [SerializeField] private float _slotHintEase = 0.25f;

        [Header("Coming home")]
        [Tooltip("Colour of the lander's lamps.")]
        [SerializeField] private Color _lampColor = Palette.Get(PaletteSwatch.WarmLamp);

        [Tooltip("Lamp brightness when 07 is home.")]
        [Range(0f, 10f)] [SerializeField] private float _lampIntensity = 2f;

        [Tooltip("Lamp range (m).")]
        [Range(1f, 40f)] [SerializeField] private float _lampRange = 9f;

        [Tooltip("Within this distance (m) of the lander the base is fully warm...")]
        [Range(5f, 100f)] [SerializeField] private float _warmNear = 25f;

        [Tooltip("...and from this distance (m) on it rests at its far warmth.")]
        [Range(10f, 400f)] [SerializeField] private float _warmFar = 90f;

        [Tooltip("Share of the warmth the base keeps while 07 is far away (a light left on).")]
        [Range(0f, 1f)] [SerializeField] private float _farWarmth = 0.35f;

        [Tooltip("Seconds (time constant) for the base to warm up or cool down.")]
        [Range(0f, 10f)] [SerializeField] private float _warmEase = 1.5f;

        [Tooltip("Window glow (emission × this) when fully warm.")]
        [Range(0f, 4f)] [SerializeField] private float _windowGlow = 1.4f;

        [Tooltip("Shelf light strip glow when fully warm.")]
        [Range(0f, 4f)] [SerializeField] private float _shelfGlow = 1.2f;

        public Vector3 LanderOffset => _landerOffset;
        public float DepositRadius => _depositRadius;
        public float DepositHeight => _depositHeight;
        public float DepositDuration => _depositDuration;
        public float DepositLift => _depositLift;
        public float DepositHover => _depositHover;
        public float SettleOvershoot => _settleOvershoot;
        public int DepositGift => _depositGift;
        public float SlotHintRadius => _slotHintRadius;
        public float SlotHintIntensity => _slotHintIntensity;
        public float SlotHintEase => _slotHintEase;
        public Color LampColor => _lampColor;
        public float LampIntensity => _lampIntensity;
        public float LampRange => _lampRange;
        public float WarmNear => _warmNear;
        public float WarmFar => Mathf.Max(_warmFar, _warmNear + 1f);
        public float FarWarmth => _farWarmth;
        public float WarmEase => _warmEase;
        public float WindowGlow => _windowGlow;
        public float ShelfGlow => _shelfGlow;

        /// <summary>0..1 warmth target for 07 <paramref name="distance"/> metres from the lander.</summary>
        public float WarmthAt(float distance)
        {
            return Mathf.Lerp(1f, _farWarmth, Ease.Step(_warmNear, WarmFar, distance));
        }
    }
}
