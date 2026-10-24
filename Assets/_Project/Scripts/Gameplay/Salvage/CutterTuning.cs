using UnityEngine;
using MoonProject.Core;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The cutter's tiers (docs/features/M3-26 §3): tier 0, the old cutter, then one row per kit that improves it. A
    /// tier is shorter to charge and quicker and cheaper to cut, never skippable. Pure data.
    /// </summary>
    [CreateAssetMenu(menuName = "MoonProject/Gameplay/Cutter Tuning", fileName = "CutterTuning")]
    public sealed class CutterTuning : ScriptableObject
    {
        [Tooltip("Tier 0 (the old cutter), then each kit's tier in order (tier 1: the Focus Lens).")]
        [SerializeField] private CutterTier[] _tiers =
        {
            new CutterTier(0.35f, 1f, 1f, 5), new CutterTier(0.28f, 1.3f, 0.85f, 7),
        };

        [Tooltip("Seconds 07 plants itself before a fresh charge builds (every tier: upgrades shorten the stages).")]
        [Range(0f, 2f)] [SerializeField] private float _braceSeconds = 0.5f;

        [Tooltip("Seconds each stage holds still once it locks, before the next one builds: every stage is felt.")]
        [Range(0f, 0.5f)] [SerializeField] private float _lockHoldSeconds = 0.15f;

        [Tooltip("Seconds of stillness after the full lock, before the beam fires (the held breath): the release is " +
                 "a beat, not an instant.")]
        [Range(0f, 1f)] [SerializeField] private float _breathSeconds = 0.35f;

        [Tooltip("A re-press at the same site within this many seconds of the last firing finds the emitter warm.")]
        [Range(0f, 20f)] [SerializeField] private float _warmWindowSeconds = 6f;

        [Tooltip("A warm emitter skips the brace and builds its stages this many times as long.")]
        [Range(0.2f, 1f)] [SerializeField] private float _warmFactor = 0.6f;

        [Tooltip("The kit that brings each tier above 0, in order (one fewer than the tiers).")]
        [SerializeField] private RoverAbility[] _kits = { RoverAbility.FocusLens };

        public int TierCount => _tiers.Length;
        public float BraceSeconds => _braceSeconds;
        public float LockHoldSeconds => _lockHoldSeconds;
        public float BreathSeconds => _breathSeconds;
        public float WarmWindowSeconds => _warmWindowSeconds;
        public float WarmFactor => _warmFactor;

        public CutterTier Tier(int index)
        {
            return _tiers[Mathf.Clamp(index, 0, _tiers.Length - 1)];
        }

        /// <summary>The kit that brings tier <paramref name="tier"/> (1 or more).</summary>
        public RoverAbility KitFor(int tier)
        {
            return _kits[tier - 1];
        }

        private void OnValidate()
        {
            if (_tiers.Length != _kits.Length + 1)
            {
                Debug.LogError($"{nameof(CutterTuning)}: {_tiers.Length} tiers need {_tiers.Length - 1} kits, not " +
                               $"{_kits.Length}.", this);
            }
        }
    }
}
