using UnityEngine;

namespace MoonProject.Gameplay.PlayModeTests
{
    /// <summary>What the game camera sees of a point near 07: whether 07's drawn body hides it, and where it sits.</summary>
    public static class RoverSight
    {
        /// <summary>
        /// A mesh whose own box is larger than any part of 07 is the tracks' deforming mesh, whose bounds follow the
        /// rover's world position, not its shape.
        /// </summary>
        private const float LargestPartMetres = 4f;

        /// <summary>
        /// The name of the piece of 07's drawn body (each mesh's own box, not its physics) standing between
        /// <paramref name="eye"/> and <paramref name="point"/>, or null: 07's colliders are a low chassis, so a physics
        /// ray says clear while its head hides the point.
        /// </summary>
        public static string HiddenBy(Transform rover, Vector3 eye, Vector3 point)
        {
            foreach (Renderer renderer in rover.GetComponentsInChildren<Renderer>())
            {
                if (!renderer.enabled || !renderer.gameObject.activeInHierarchy)
                {
                    continue;
                }

                if (!renderer.TryGetComponent(out MeshFilter filter) || filter.sharedMesh == null)
                {
                    continue;
                }

                Bounds local = filter.sharedMesh.bounds;
                if (local.size.magnitude > LargestPartMetres)
                {
                    continue;
                }

                Transform node = renderer.transform;
                Vector3 from = node.InverseTransformPoint(eye);
                Vector3 along = node.InverseTransformPoint(point) - from;
                if (local.IntersectRay(new Ray(from, along), out float distance) && distance <= along.magnitude)
                {
                    return $"{renderer.name} (box {local.size.x:0.00} x {local.size.y:0.00} x {local.size.z:0.00})";
                }
            }

            return null;
        }

        /// <summary>
        /// True when <paramref name="point"/> is in front of <paramref name="view"/> and inside the middle
        /// <paramref name="share"/> of the screen both ways (0.7 = the middle 70%); <paramref name="viewport"/> is
        /// where it sits.
        /// </summary>
        public static bool IsCentral(Camera view, Vector3 point, float share, out Vector3 viewport)
        {
            viewport = view.WorldToViewportPoint(point);
            float margin = (1f - share) * 0.5f;
            return viewport.z > 0f && viewport.x >= margin && viewport.x <= 1f - margin && viewport.y >= margin
                && viewport.y <= 1f - margin;
        }
    }
}
