using System;
using System.Collections.Generic;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Designer-facing tuning for 07's freeform base building (0.5.7): placement distance bounds from 07, soft-snap
    /// distance and angle tolerances, welding range, deconstruction material refund share, and the optional playtest
    /// unlock list applied at initialization.
    /// </summary>
    [CreateAssetMenu(menuName = "MoonProject/Gameplay/Building Tuning", fileName = "BuildingTuning")]
    public sealed class BuildingTuning : ScriptableObject
    {
        [Tooltip("Closest horizontal distance (m) from 07's pivot at which a structure centre may be placed.")]
        [Range(0.5f, 10f)] [SerializeField] private float _minPlacementDistance = 2.0f;

        [Tooltip("Furthest horizontal distance (m) from 07's pivot at which a structure centre may be placed.")]
        [Range(3f, 40f)] [SerializeField] private float _maxPlacementDistance = 12.0f;

        [Tooltip("Maximum horizontal distance (m) within which a candidate softly snaps to a neighbour's port or edge.")]
        [Range(0.05f, 3f)] [SerializeField] private float _snapDistance = 0.6f;

        [Tooltip("Maximum angular difference (degrees) within which a candidate softly snaps to a neighbour's port or edge.")]
        [Range(1f, 45f)] [SerializeField] private float _snapAngleDegrees = 20f;

        [Tooltip("Maximum horizontal distance (m) from a structure's pivot within which 07's welding beam can build it.")]
        [Range(1f, 20f)] [SerializeField] private float _weldRange = 6.0f;

        [Tooltip("Share (0..1) of a structure's Recipe build cost returned when 07 deconstructs it.")]
        [Range(0f, 1f)] [SerializeField] private float _deconstructRefundShare = 0.75f;

        [Tooltip("Structure IDs unlocked at game start for playtesting (empty by default so a new game knows no structures).")]
        [SerializeField] private string[] _playtestUnlockedIds = Array.Empty<string>();

        public float MinPlacementDistance => _minPlacementDistance;

        public float MaxPlacementDistance => _maxPlacementDistance;

        public float SnapDistance => _snapDistance;

        public float SnapAngleDegrees => _snapAngleDegrees;

        public float WeldRange => _weldRange;

        public float DeconstructRefundShare => _deconstructRefundShare;

        /// <summary>
        /// Structure IDs configured in data to start unlocked for playtests or capture sessions (empty by default in a
        /// normal game).
        /// </summary>
        public IReadOnlyList<string> PlaytestUnlockedIds => _playtestUnlockedIds ?? Array.Empty<string>();

        /// <summary>Populates tuning parameters (used by tests and tuning builders).</summary>
        public void Populate(
            float minPlacementDistance,
            float maxPlacementDistance,
            float snapDistance,
            float snapAngleDegrees,
            float weldRange,
            float deconstructRefundShare,
            string[] playtestUnlockedIds = null)
        {
            _minPlacementDistance = minPlacementDistance;
            _maxPlacementDistance = maxPlacementDistance;
            _snapDistance = snapDistance;
            _snapAngleDegrees = snapAngleDegrees;
            _weldRange = weldRange;
            _deconstructRefundShare = Mathf.Clamp01(deconstructRefundShare);
            _playtestUnlockedIds = playtestUnlockedIds ?? Array.Empty<string>();
        }

        /// <summary>Sets the data-driven playtest unlock structure IDs (empty by default).</summary>
        public void SetPlaytestUnlockedIds(params string[] structureIds)
        {
            _playtestUnlockedIds = structureIds ?? Array.Empty<string>();
        }

        private void OnValidate()
        {
            if (_maxPlacementDistance < _minPlacementDistance)
            {
                _maxPlacementDistance = _minPlacementDistance;
            }

            _deconstructRefundShare = Mathf.Clamp01(_deconstructRefundShare);
            if (_playtestUnlockedIds == null)
            {
                _playtestUnlockedIds = Array.Empty<string>();
            }
        }
    }
}
