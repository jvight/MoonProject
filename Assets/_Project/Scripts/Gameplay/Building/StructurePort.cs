using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// Cable or snap port on a <see cref="StructureDefinition"/>: local position relative to the structure's pivot and
    /// outward horizontal direction in local space.
    /// </summary>
    [Serializable]
    public struct StructurePort : IEquatable<StructurePort>
    {
        [Tooltip("Port position in the structure's local space (metres from the pivot).")]
        [SerializeField] private Vector3 _localPosition;

        [Tooltip("Outward horizontal direction in the structure's local space (non-zero in XZ).")]
        [SerializeField] private Vector3 _localDirection;

        public StructurePort(Vector3 localPosition, Vector3 localDirection)
        {
            _localPosition = localPosition;
            Vector3 horizontal = new Vector3(localDirection.x, 0f, localDirection.z);
            _localDirection = horizontal.sqrMagnitude > 1e-6f ? horizontal.normalized : Vector3.zero;
        }

        public Vector3 LocalPosition => _localPosition;

        public Vector3 LocalDirection => _localDirection;

        /// <summary>True when <see cref="LocalDirection"/> has a valid non-zero horizontal direction.</summary>
        public bool HasValidDirection =>
            _localDirection.x * _localDirection.x + _localDirection.z * _localDirection.z > 0.5f;

        /// <summary>World-space position of this port when its structure is placed at <paramref name="pose"/>.</summary>
        public Vector3 WorldPosition(StructurePose pose)
        {
            return pose.TransformPoint(_localPosition);
        }

        /// <summary>World-space outward horizontal unit direction when placed at <paramref name="pose"/>.</summary>
        public Vector3 WorldDirection(StructurePose pose)
        {
            Vector3 rotated = pose.TransformDirection(_localDirection);
            Vector3 horizontal = new Vector3(rotated.x, 0f, rotated.z);
            return horizontal.sqrMagnitude > 1e-6f ? horizontal.normalized : Vector3.forward;
        }

        public bool Equals(StructurePort other)
        {
            return _localPosition.Equals(other._localPosition) && _localDirection.Equals(other._localDirection);
        }

        public override bool Equals(object obj)
        {
            return obj is StructurePort other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_localPosition, _localDirection);
        }
    }
}
