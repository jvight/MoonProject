using UnityEngine;

namespace MoonProject.Gameplay
{
    /// <summary>
    /// The player's view as pure maths (a perspective camera's pose, vertical field of view and aspect), so "could the
    /// player see this?" can be answered and tested without a Camera. Ignores occlusion: anything it says is unseen
    /// really is off screen.
    /// </summary>
    public readonly struct ViewFrustum
    {
        private readonly Vector3 _position;
        private readonly Quaternion _inverseRotation;
        private readonly float _cosHorizontal;
        private readonly float _sinHorizontal;
        private readonly float _cosVertical;
        private readonly float _sinVertical;

        /// <param name="verticalFieldOfView">Full vertical angle in degrees (Camera.fieldOfView).</param>
        /// <param name="aspect">Width over height (Camera.aspect).</param>
        public ViewFrustum(Vector3 position, Quaternion rotation, float verticalFieldOfView, float aspect)
        {
            _position = position;
            _inverseRotation = Quaternion.Inverse(rotation);
            float vertical = 0.5f * verticalFieldOfView * Mathf.Deg2Rad;
            float horizontal = Mathf.Atan(Mathf.Tan(vertical) * aspect);
            _cosVertical = Mathf.Cos(vertical);
            _sinVertical = Mathf.Sin(vertical);
            _cosHorizontal = Mathf.Cos(horizontal);
            _sinHorizontal = Mathf.Sin(horizontal);
        }

        public static ViewFrustum Of(Camera camera)
        {
            Transform pose = camera.transform;
            return new ViewFrustum(pose.position, pose.rotation, camera.fieldOfView, camera.aspect);
        }

        /// <summary>True when any part of the sphere at <paramref name="centre"/> could be on screen.</summary>
        public bool Sees(Vector3 centre, float radius)
        {
            Vector3 local = _inverseRotation * (centre - _position);
            if (local.z < -radius)
            {
                return false;
            }

            // Each side plane passes through the eye; its outward normal leans back from the view axis.
            float right = local.x * _cosHorizontal - local.z * _sinHorizontal;
            float left = -local.x * _cosHorizontal - local.z * _sinHorizontal;
            float top = local.y * _cosVertical - local.z * _sinVertical;
            float bottom = -local.y * _cosVertical - local.z * _sinVertical;
            return right <= radius && left <= radius && top <= radius && bottom <= radius;
        }
    }
}
