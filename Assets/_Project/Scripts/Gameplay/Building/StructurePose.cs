using System;
using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// World placement pose of a base structure on the lunar surface: world position and heading yaw in degrees
    /// measured clockwise from +Z toward +X (matching <see cref="SurfaceRules.BearingDirection"/> and
    /// <c>Quaternion.Euler(0f, Yaw, 0f)</c>).
    /// </summary>
    [Serializable]
    public struct StructurePose : IEquatable<StructurePose>
    {
        private const float AngleEpsilon = 1e-4f;
        private const float PositionEpsilonSq = 1e-8f;

        [Tooltip("World-space pivot position on the terrain.")]
        [SerializeField] private Vector3 _position;

        [Tooltip("Heading angle in degrees around +Y (0 faces +Z, 90 faces +X).")]
        [SerializeField] private float _yaw;

        public StructurePose(Vector3 position, float yaw)
        {
            _position = position;
            _yaw = NormalizeYaw(yaw);
        }

        public Vector3 Position => _position;

        public float Yaw => _yaw;

        public Quaternion Rotation => Quaternion.Euler(0f, _yaw, 0f);

        /// <summary>Unit horizontal forward (+Z in local space).</summary>
        public Vector3 Forward => SurfaceRules.BearingDirection(_yaw);

        /// <summary>Unit horizontal right (+X in local space).</summary>
        public Vector3 Right
        {
            get
            {
                float radians = _yaw * Mathf.Deg2Rad;
                return new Vector3(Mathf.Cos(radians), 0f, -Mathf.Sin(radians));
            }
        }

        /// <summary>Transforms a local offset into world space using this pose's position and horizontal yaw.</summary>
        public Vector3 TransformPoint(Vector3 localPoint)
        {
            float radians = _yaw * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            return new Vector3(
                _position.x + localPoint.x * cos + localPoint.z * sin,
                _position.y + localPoint.y,
                _position.z - localPoint.x * sin + localPoint.z * cos);
        }

        /// <summary>Rotates a local direction vector into world space using this pose's horizontal yaw.</summary>
        public Vector3 TransformDirection(Vector3 localDirection)
        {
            float radians = _yaw * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            return new Vector3(
                localDirection.x * cos + localDirection.z * sin,
                localDirection.y,
                -localDirection.x * sin + localDirection.z * cos);
        }

        /// <summary>Transforms a world-space point into this pose's local coordinates.</summary>
        public Vector3 InverseTransformPoint(Vector3 worldPoint)
        {
            float radians = _yaw * Mathf.Deg2Rad;
            float sin = Mathf.Sin(radians);
            float cos = Mathf.Cos(radians);
            float dx = worldPoint.x - _position.x;
            float dy = worldPoint.y - _position.y;
            float dz = worldPoint.z - _position.z;
            return new Vector3(
                dx * cos - dz * sin,
                dy,
                dx * sin + dz * cos);
        }

        /// <summary>Normalizes a yaw angle in degrees into [0, 360).</summary>
        public static float NormalizeYaw(float degrees)
        {
            float wrapped = Mathf.Repeat(degrees, 360f);
            return wrapped >= 360f - AngleEpsilon ? 0f : wrapped;
        }

        /// <summary>Unsigned shortest angular distance in degrees [0, 180] between two yaw angles.</summary>
        public static float DeltaYaw(float a, float b)
        {
            return Mathf.Abs(Mathf.DeltaAngle(a, b));
        }

        public bool Equals(StructurePose other)
        {
            return (_position - other._position).sqrMagnitude <= PositionEpsilonSq &&
                   DeltaYaw(_yaw, other._yaw) <= AngleEpsilon;
        }

        public override bool Equals(object obj)
        {
            return obj is StructurePose other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_position, _yaw);
        }
    }
}
