using UnityEngine;

namespace MoonProject.Rover
{
    /// <summary>
    /// The radius 07's wheels roll on (owner playtest f3d1ff265: the wheels read faster than 07 travels). It is the
    /// wheel mesh's own, measured, so a model change can never leave the spin out of step with the ground.
    /// </summary>
    public static class WheelKinematics
    {
        /// <summary>
        /// A wheel's drawn turn rate for its true one: exactly the same up to <paramref name="knee"/> (deg/s), then
        /// easing toward <paramref name="ceiling"/> (a tanh over the span between them, its slope 1 at the knee so there
        /// is no corner), so ordinary driving turns the wheels the true amount and only a fast wheel is tamed, never
        /// frantic on screen. Odd about zero.
        /// </summary>
        public static float SoftCap(float degreesPerSecond, float knee, float ceiling)
        {
            float magnitude = Mathf.Abs(degreesPerSecond);
            if (magnitude <= knee)
            {
                return degreesPerSecond;
            }

            float span = Mathf.Max(ceiling - knee, 1e-3f);
            float capped = knee + span * (float)System.Math.Tanh((magnitude - knee) / span);
            return Mathf.Sign(degreesPerSecond) * capped;
        }

        /// <summary>
        /// The wheel's radius (m): how far its mesh reaches from the hub across the axle (<paramref name="wheel"/>'s
        /// local X), measured in the world as the farthest the mesh's box extends up or forward of the hub. Zero when it
        /// has no mesh.
        /// </summary>
        public static float Radius(Transform wheel)
        {
            float reach = 0f;
            foreach (MeshFilter filter in wheel.GetComponentsInChildren<MeshFilter>(true))
            {
                Mesh mesh = filter.sharedMesh;
                if (mesh == null)
                {
                    continue;
                }

                Bounds bounds = mesh.bounds;
                Vector3 centre = filter.transform.TransformPoint(bounds.center) - wheel.position;
                Vector3 halfX = filter.transform.TransformVector(Vector3.right * bounds.extents.x);
                Vector3 halfY = filter.transform.TransformVector(Vector3.up * bounds.extents.y);
                Vector3 halfZ = filter.transform.TransformVector(Vector3.forward * bounds.extents.z);
                reach = Mathf.Max(reach, Reach(centre, halfX, halfY, halfZ, wheel.up),
                    Reach(centre, halfX, halfY, halfZ, wheel.forward));
            }

            return reach;
        }

        private static float Reach(Vector3 centre, Vector3 halfX, Vector3 halfY, Vector3 halfZ, Vector3 direction)
        {
            return Mathf.Abs(Vector3.Dot(centre, direction)) + Mathf.Abs(Vector3.Dot(halfX, direction))
                + Mathf.Abs(Vector3.Dot(halfY, direction)) + Mathf.Abs(Vector3.Dot(halfZ, direction));
        }
    }
}
