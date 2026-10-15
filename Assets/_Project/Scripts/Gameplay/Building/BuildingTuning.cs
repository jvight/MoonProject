using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Designer-facing tuning for 07's freeform base building (0.5.7): placement distance bounds from 07, soft-snap
    /// distance and angle tolerances, welding range, and deconstruction material refund share.
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

        public float MinPlacementDistance => _minPlacementDistance;

        public float MaxPlacementDistance => _maxPlacementDistance;

        public float SnapDistance => _snapDistance;

        public float SnapAngleDegrees => _snapAngleDegrees;

        public float WeldRange => _weldRange;

        public float DeconstructRefundShare => _deconstructRefundShare;

        /// <summary>Populates tuning parameters (used by tests and tuning builders).</summary>
        public void Populate(
            float minPlacementDistance,
            float maxPlacementDistance,
            float snapDistance,
            float snapAngleDegrees,
            float weldRange,
            float deconstructRefundShare)
        {
            _minPlacementDistance = minPlacementDistance;
            _maxPlacementDistance = maxPlacementDistance;
            _snapDistance = snapDistance;
            _snapAngleDegrees = snapAngleDegrees;
            _weldRange = weldRange;
            _deconstructRefundShare = Mathf.Clamp01(deconstructRefundShare);
        }
    }
}
