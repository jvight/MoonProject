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
            new CutterTier(0.65f, 0.3f, 1f, 1f, 5), new CutterTier(0.45f, 0.2f, 1.3f, 0.85f, 7),
        };

        [Tooltip("The kit that brings each tier above 0, in order (one fewer than the tiers).")]
        [SerializeField] private RoverAbility[] _kits = { RoverAbility.FocusLens };

        public int TierCount => _tiers.Length;

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
