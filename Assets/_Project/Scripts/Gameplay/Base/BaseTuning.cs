using UnityEngine;
using UnityEngine.Serialization;
using MoonProject.Art;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Home: where the lander stands, how relics are taken onto the museum shelf, the scrap gift for each memory
    /// brought home, how the base lights the ground as 07 approaches, and how it carries across the basin while 07 is
    /// far away (windows that never dim, a soft amber halo). Created by the Gameplay/Tuning builder; runtime code only
    /// reads it.
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

        [Tooltip("Within this distance (m) of the lander 07 is home: the lamps light the ground fully...")]
        [Range(5f, 100f)] [SerializeField] private float _warmNear = 25f;

        [Tooltip("...and from this distance (m) on 07 is far away.")]
        [Range(10f, 400f)] [SerializeField] private float _warmFar = 90f;

        [Tooltip("Share of the lamps' light on the ground (and of the shelf strip) kept while 07 is far away: a " +
                 "light left on. The windows do not dim.")]
        [FormerlySerializedAs("_farWarmth")]
        [Range(0f, 1f)] [SerializeField] private float _farLampWarmth = 0.35f;

        [Tooltip("Seconds (time constant) for the base to follow 07 near or far.")]
        [Range(0f, 10f)] [SerializeField] private float _warmEase = 1.5f;

        [Tooltip("Window glow while 07 is home (linear emission multiplier, 1 = as authored).")]
        [Range(0f, 4f)] [SerializeField] private float _windowGlow = 1f;

        [Tooltip("The windows glow this many times brighter while 07 is far away: home calls it back.")]
        [Range(1f, 2f)] [SerializeField] private float _farWindowBoost = 1.2f;

        [Tooltip("Shelf light strip glow while 07 is home (linear emission multiplier, 1 = as authored).")]
        [Range(0f, 4f)] [SerializeField] private float _shelfGlow = 0.6f;

        [Header("Home halo")]
        [Tooltip("Centre of the soft amber glow over home, in lander space (m).")]
        [SerializeField] private Vector3 _haloCentre = new Vector3(0f, 3f, 0f);

        [Tooltip("Camera distance (m) where the halo starts to show (invisible closer in)...")]
        [Range(10f, 300f)] [SerializeField] private float _haloAppear = 70f;

        [Tooltip("...and where it reaches its full glow.")]
        [Range(20f, 600f)] [SerializeField] private float _haloFull = 200f;

        [Tooltip("Radius (m) of the halo where it starts to show.")]
        [Range(0.5f, 30f)] [SerializeField] private float _haloRadius = 5f;

        [Tooltip("Metres of radius the halo gains per metre of distance beyond that, so on screen it shrinks " +
                 "slower than home does.")]
        [Range(0f, 0.2f)] [SerializeField] private float _haloGrowth = 0.035f;

        [Tooltip("Brightness at full glow: soft, under the bloom threshold (the windows are the bright points).")]
        [Range(0f, 2f)] [SerializeField] private float _haloGlow = 0.55f;

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
        public float FarLampWarmth => _farLampWarmth;
        public float WarmEase => _warmEase;
        public float WindowGlow => _windowGlow;
        public float FarWindowBoost => _farWindowBoost;
        public float ShelfGlow => _shelfGlow;
        public Vector3 HaloCentre => _haloCentre;
        public float HaloAppear => _haloAppear;
        public float HaloFull => Mathf.Max(_haloFull, _haloAppear + 1f);
        public float HaloRadius => _haloRadius;
        public float HaloGlow => _haloGlow;

        /// <summary>0 while 07 is home .. 1 once it is <see cref="WarmFar"/> or more from the lander.</summary>
        public float FarnessAt(float distance)
        {
            return Ease.Step(_warmNear, WarmFar, distance);
        }

        /// <summary>0..1 share of the lamps' light (and of the shelf strip's glow) at <paramref name="farness"/>.
        /// </summary>
        public float LampWarmth(float farness)
        {
            return Mathf.Lerp(1f, _farLampWarmth, Mathf.Clamp01(farness));
        }

        /// <summary>Window glow at <paramref name="farness"/>: full at home, a little brighter far away.</summary>
        public float WindowGlowAt(float farness)
        {
            return _windowGlow * Mathf.Lerp(1f, _farWindowBoost, Mathf.Clamp01(farness));
        }

        /// <summary>Radius (m) of the home halo seen from <paramref name="distance"/> metres away.</summary>
        public float HaloRadiusAt(float distance)
        {
            return _haloRadius + _haloGrowth * Mathf.Max(0f, distance - _haloAppear);
        }

        /// <summary>Glow of the home halo seen from <paramref name="distance"/> metres away (0 up close).</summary>
        public float HaloGlowAt(float distance)
        {
            return _haloGlow * Ease.Step(_haloAppear, HaloFull, distance);
        }
    }
}
